using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Common.Models;
using LaoCai.SoftwareManagement.Domain.Entities.Deployments;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class DeploymentService : IDeploymentService
{
    private readonly IAppDbContext _context;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly IBackgroundJobService _jobService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeploymentService(
        IAppDbContext context,
        IScopeAuthorizationService scopeAuth,
        IBackgroundJobService jobService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _scopeAuth = scopeAuth;
        _jobService = jobService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<DeploymentDto>> GetDeploymentsAsync(
        DeploymentFilterDto filter,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        // Scope check
        var allowedOrgsForRead = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId, "deployments.read", cancellationToken);
        var allowedOrgsForDrafts = await _scopeAuth.GetAllowedOrganizationIdsAsync(currentUserId, "deployments.read_drafts", cancellationToken);

        // If user has neither permission anywhere, return empty
        if (allowedOrgsForRead != null && allowedOrgsForRead.Count == 0 &&
            allowedOrgsForDrafts != null && allowedOrgsForDrafts.Count == 0)
        {
            return PagedResult<DeploymentDto>.Create(new List<DeploymentDto>(), 0, page, pageSize);
        }

        var query = _context.Deployments
            .AsNoTracking()
            .Include(d => d.Software)
            .Include(d => d.Organization)
            .Include(d => d.CurrentApprovedRevision)
                .ThenInclude(r => r!.Release)
            .Include(d => d.CurrentApprovedRevision)
                .ThenInclude(r => r!.ResponsibleUser)
            .Include(d => d.Revisions.Where(r => r.WorkflowStatus == "Draft" || r.WorkflowStatus == "Submitted"))
                .ThenInclude(r => r.Release)
            .Include(d => d.Revisions.Where(r => r.WorkflowStatus == "Draft" || r.WorkflowStatus == "Submitted"))
                .ThenInclude(r => r.ResponsibleUser)
            .AsQueryable();

        // Filter by organization scope
        if (allowedOrgsForRead != null || allowedOrgsForDrafts != null)
        {
            var allAllowedOrgs = new HashSet<Guid>();
            if (allowedOrgsForRead != null)
                allAllowedOrgs.UnionWith(allowedOrgsForRead);
            if (allowedOrgsForDrafts != null)
                allAllowedOrgs.UnionWith(allowedOrgsForDrafts);

            query = query.Where(d => allAllowedOrgs.Contains(d.OrganizationId));
        }

        // Apply filters
        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(d => d.OrganizationId == filter.OrganizationId.Value);
        }

        if (filter.SoftwareId.HasValue)
        {
            query = query.Where(d => d.SoftwareId == filter.SoftwareId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Environment))
        {
            query = query.Where(d => d.Environment == filter.Environment);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(d =>
                d.Software.Name.ToLower().Contains(search) ||
                d.Software.Code.ToLower().Contains(search) ||
                d.InstanceKey.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var deployments = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var resultItems = new List<DeploymentDto>();

        foreach (var d in deployments)
        {
            var canViewDraftsInOrg = allowedOrgsForDrafts == null || allowedOrgsForDrafts.Contains(d.OrganizationId);

            // If user can only read approved, but deployment has no approved revision, skip (or don't show drafts)
            if (!canViewDraftsInOrg && d.CurrentApprovedRevisionId == null)
            {
                continue;
            }

            var activeRev = canViewDraftsInOrg
                ? d.Revisions.FirstOrDefault(r => r.WorkflowStatus == "Draft" || r.WorkflowStatus == "Submitted")
                : null;

            // Filter by operational status
            if (!string.IsNullOrWhiteSpace(filter.OperationalStatus))
            {
                var effectiveStatus = activeRev?.OperationalStatus ?? d.CurrentApprovedRevision?.OperationalStatus;
                if (effectiveStatus != filter.OperationalStatus)
                    continue;
            }

            // Filter by workflow status
            if (!string.IsNullOrWhiteSpace(filter.WorkflowStatus))
            {
                var currentWfStatus = activeRev?.WorkflowStatus ?? (d.CurrentApprovedRevisionId != null ? "Approved" : null);
                if (currentWfStatus != filter.WorkflowStatus)
                    continue;
            }

            resultItems.Add(MapToDeploymentDto(d, activeRev));
        }

        return PagedResult<DeploymentDto>.Create(resultItems, totalCount, page, pageSize);
    }

    public async Task<DeploymentDto> GetDeploymentByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var deployment = await _context.Deployments
            .AsNoTracking()
            .Include(d => d.Software)
            .Include(d => d.Organization)
            .Include(d => d.CurrentApprovedRevision)
                .ThenInclude(r => r!.Release)
            .Include(d => d.CurrentApprovedRevision)
                .ThenInclude(r => r!.ResponsibleUser)
            .Include(d => d.Revisions)
                .ThenInclude(r => r.Release)
            .Include(d => d.Revisions)
                .ThenInclude(r => r.ResponsibleUser)
            .Include(d => d.Revisions)
                .ThenInclude(r => r.Decisions)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (deployment == null)
            throw new NotFoundException("Hồ sơ triển khai", id);

        var hasRead = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.read", deployment.OrganizationId, cancellationToken);
        var hasReadDrafts = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.read_drafts", deployment.OrganizationId, cancellationToken);

        if (!hasRead && !hasReadDrafts)
            throw new NotFoundException("Hồ sơ triển khai", id);

        if (!hasReadDrafts && deployment.CurrentApprovedRevisionId == null)
            throw new NotFoundException("Hồ sơ triển khai", id);

        var activeRev = hasReadDrafts
            ? deployment.Revisions.FirstOrDefault(r => r.WorkflowStatus == "Draft" || r.WorkflowStatus == "Submitted" || r.WorkflowStatus == "Rejected")
            : null;

        return MapToDeploymentDto(deployment, activeRev);
    }

    public async Task<DeploymentRevisionDto> GetRevisionByIdAsync(Guid revisionId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var revision = await _context.DeploymentRevisions
            .AsNoTracking()
            .Include(r => r.Deployment)
            .Include(r => r.Release)
            .Include(r => r.ResponsibleUser)
            .Include(r => r.SubmittedByUser)
            .Include(r => r.Decisions)
                .ThenInclude(dec => dec.Actor)
            .FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);

        if (revision == null)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        var hasRead = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.read", revision.Deployment.OrganizationId, cancellationToken);
        var hasReadDrafts = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.read_drafts", revision.Deployment.OrganizationId, cancellationToken);

        if (!hasRead && !hasReadDrafts)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        if (revision.WorkflowStatus != "Approved" && !hasReadDrafts)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        return MapToRevisionDto(revision);
    }

    public async Task<List<DeploymentRevisionDto>> GetRevisionHistoryAsync(Guid deploymentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var deployment = await _context.Deployments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deploymentId, cancellationToken);

        if (deployment == null)
            throw new NotFoundException("Hồ sơ triển khai", deploymentId);

        var hasRead = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.read", deployment.OrganizationId, cancellationToken);
        var hasReadDrafts = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.read_drafts", deployment.OrganizationId, cancellationToken);

        if (!hasRead && !hasReadDrafts)
            throw new NotFoundException("Hồ sơ triển khai", deploymentId);

        var query = _context.DeploymentRevisions
            .AsNoTracking()
            .Where(r => r.DeploymentId == deploymentId)
            .Include(r => r.Release)
            .Include(r => r.ResponsibleUser)
            .Include(r => r.SubmittedByUser)
            .Include(r => r.Decisions)
                .ThenInclude(dec => dec.Actor)
            .OrderByDescending(r => r.RevisionNo)
            .AsQueryable();

        if (!hasReadDrafts)
        {
            query = query.Where(r => r.WorkflowStatus == "Approved");
        }

        var revisions = await query.ToListAsync(cancellationToken);
        return revisions.Select(MapToRevisionDto).ToList();
    }

    public async Task<DeploymentDto> CreateDeploymentAsync(CreateDeploymentDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var hasWrite = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.write", dto.OrganizationId, cancellationToken);
        if (!hasWrite)
            throw new ForbiddenException("Bạn không có quyền tạo hồ sơ triển khai tại đơn vị này.");

        // Validate software
        var software = await _context.Software.FirstOrDefaultAsync(s => s.Id == dto.SoftwareId, cancellationToken);
        if (software == null)
            throw new CustomValidationException("SoftwareId", "Phần mềm được chọn không tồn tại.");

        var instanceKey = string.IsNullOrWhiteSpace(dto.InstanceKey) ? "default" : dto.InstanceKey.Trim();

        // Check duplicate
        var exists = await _context.Deployments.AnyAsync(d =>
            d.SoftwareId == dto.SoftwareId &&
            d.OrganizationId == dto.OrganizationId &&
            d.Environment == dto.Environment &&
            d.InstanceKey == instanceKey, cancellationToken);

        if (exists)
            throw new ConflictException($"Hồ sơ triển khai cho phần mềm '{software.Name}' tại môi trường '{dto.Environment}' (instance: '{instanceKey}') đã tồn tại.");

        ValidateDates(dto.OperationalStatus, dto.StartDate, dto.GoLiveDate);

        var nowUtc = _dateTimeProvider.UtcNow;
        var deploymentId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();

        var deployment = new Deployment
        {
            Id = deploymentId,
            SoftwareId = dto.SoftwareId,
            OrganizationId = dto.OrganizationId,
            Environment = dto.Environment,
            InstanceKey = instanceKey,
            CurrentApprovedRevisionId = null,
            Version = 1,
            CreatedAt = nowUtc,
            CreatedBy = currentUserId
        };

        var revision = new DeploymentRevision
        {
            Id = revisionId,
            DeploymentId = deploymentId,
            RevisionNo = 1,
            ReleaseId = dto.ReleaseId,
            OperationalStatus = string.IsNullOrWhiteSpace(dto.OperationalStatus) ? "NotInUse" : dto.OperationalStatus,
            StartDate = dto.StartDate,
            GoLiveDate = dto.GoLiveDate,
            ResponsibleUserId = dto.ResponsibleUserId,
            WorkflowStatus = "Draft",
            Version = 1,
            CreatedAt = nowUtc,
            CreatedBy = currentUserId
        };

        _context.Deployments.Add(deployment);
        _context.DeploymentRevisions.Add(revision);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetDeploymentByIdAsync(deploymentId, currentUserId, cancellationToken);
    }

    public async Task<DeploymentRevisionDto> UpdateDraftRevisionAsync(
        Guid revisionId,
        UpdateDraftRevisionDto dto,
        long ifMatchVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var revision = await _context.DeploymentRevisions
            .Include(r => r.Deployment)
            .FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);

        if (revision == null)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        var hasWrite = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.write", revision.Deployment.OrganizationId, cancellationToken);
        if (!hasWrite)
            throw new ForbiddenException("Bạn không có quyền sửa đổi hồ sơ triển khai tại đơn vị này.");

        if (revision.WorkflowStatus != "Draft")
            throw new CustomValidationException("WorkflowStatus", $"Chỉ bản ghi ở trạng thái 'Draft' mới được phép chỉnh sửa. Trạng thái hiện tại: {revision.WorkflowStatus}.");

        if (revision.Version != ifMatchVersion)
            throw new ConcurrencyException("Dữ liệu hồ sơ đã được cập nhật bởi một phiên làm việc khác. Vui lòng tải lại.");

        ValidateDates(dto.OperationalStatus, dto.StartDate, dto.GoLiveDate);

        if (dto.ReleaseId.HasValue)
        {
            var releaseBelongsToSoftware = await _context.SoftwareReleases
                .AnyAsync(r => r.Id == dto.ReleaseId.Value && r.SoftwareId == revision.Deployment.SoftwareId, cancellationToken);
            if (!releaseBelongsToSoftware)
                throw new CustomValidationException("ReleaseId", "Phiên bản phát hành được chọn không thuộc phần mềm của hồ sơ triển khai.");
        }

        revision.ReleaseId = dto.ReleaseId;
        revision.OperationalStatus = dto.OperationalStatus;
        revision.StartDate = dto.StartDate;
        revision.GoLiveDate = dto.GoLiveDate;
        revision.ResponsibleUserId = dto.ResponsibleUserId;
        revision.UpdatedAt = _dateTimeProvider.UtcNow;
        revision.UpdatedBy = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRevisionByIdAsync(revisionId, currentUserId, cancellationToken);
    }

    public async Task<DeploymentRevisionDto> CreateNextRevisionAsync(
        Guid deploymentId,
        long ifMatchDeploymentVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var deployment = await _context.Deployments
            .Include(d => d.Revisions)
            .Include(d => d.CurrentApprovedRevision)
            .FirstOrDefaultAsync(d => d.Id == deploymentId, cancellationToken);

        if (deployment == null)
            throw new NotFoundException("Hồ sơ triển khai", deploymentId);

        var hasWrite = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.write", deployment.OrganizationId, cancellationToken);
        if (!hasWrite)
            throw new ForbiddenException("Bạn không có quyền tạo revision mới cho hồ sơ tại đơn vị này.");

        if (deployment.Version != ifMatchDeploymentVersion)
            throw new ConcurrencyException("Hồ sơ triển khai đã được cập nhật bởi một phiên khác. Vui lòng tải lại.");

        if (deployment.CurrentApprovedRevisionId == null || deployment.CurrentApprovedRevision == null)
            throw new CustomValidationException("Deployment", "Hồ sơ chưa có bản chính thức (Approved) nên không thể tạo revision kế tiếp.");

        var hasActive = deployment.Revisions.Any(r => r.WorkflowStatus == "Draft" || r.WorkflowStatus == "Submitted");
        if (hasActive)
            throw new ConflictException("Đang có một bản nháp hoặc bản chờ duyệt đang xử lý cho hồ sơ này. Không thể tạo thêm.");

        var nextRevNo = deployment.Revisions.Max(r => r.RevisionNo) + 1;
        var approvedRev = deployment.CurrentApprovedRevision;
        var nowUtc = _dateTimeProvider.UtcNow;

        var newRevision = new DeploymentRevision
        {
            Id = Guid.NewGuid(),
            DeploymentId = deploymentId,
            RevisionNo = nextRevNo,
            ReleaseId = approvedRev.ReleaseId,
            OperationalStatus = approvedRev.OperationalStatus,
            StartDate = approvedRev.StartDate,
            GoLiveDate = approvedRev.GoLiveDate,
            ResponsibleUserId = approvedRev.ResponsibleUserId,
            WorkflowStatus = "Draft",
            Version = 1,
            CreatedAt = nowUtc,
            CreatedBy = currentUserId
        };

        _context.DeploymentRevisions.Add(newRevision);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetRevisionByIdAsync(newRevision.Id, currentUserId, cancellationToken);
    }

    public async Task<DeploymentRevisionDto> ReopenRejectedRevisionAsync(
        Guid revisionId,
        long ifMatchVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var revision = await _context.DeploymentRevisions
            .Include(r => r.Deployment)
                .ThenInclude(d => d.Revisions)
            .FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);

        if (revision == null)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        var hasWrite = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.write", revision.Deployment.OrganizationId, cancellationToken);
        if (!hasWrite)
            throw new ForbiddenException("Bạn không có quyền mở lại hồ sơ triển khai tại đơn vị này.");

        if (revision.WorkflowStatus != "Rejected")
            throw new CustomValidationException("WorkflowStatus", "Chỉ bản ghi bị từ chối ('Rejected') mới được phép mở lại (Reopen).");

        if (revision.Version != ifMatchVersion)
            throw new ConcurrencyException("Dữ liệu hồ sơ đã được cập nhật. Vui lòng tải lại trang.");

        var otherActive = revision.Deployment.Revisions
            .Any(r => r.Id != revisionId && (r.WorkflowStatus == "Draft" || r.WorkflowStatus == "Submitted"));
        if (otherActive)
            throw new ConflictException("Đang có một bản nháp hoặc bản chờ duyệt khác trên hồ sơ này. Không thể mở lại.");

        revision.WorkflowStatus = "Draft";
        revision.UpdatedAt = _dateTimeProvider.UtcNow;
        revision.UpdatedBy = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRevisionByIdAsync(revisionId, currentUserId, cancellationToken);
    }

    public async Task<DeploymentRevisionDto> SubmitRevisionAsync(
        Guid revisionId,
        long ifMatchVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var revision = await _context.DeploymentRevisions
            .Include(r => r.Deployment)
                .ThenInclude(d => d.Software)
            .Include(r => r.Deployment)
                .ThenInclude(d => d.Organization)
            .FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);

        if (revision == null)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        var hasWrite = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.write", revision.Deployment.OrganizationId, cancellationToken);
        if (!hasWrite)
            throw new ForbiddenException("Bạn không có quyền gửi duyệt hồ sơ triển khai tại đơn vị này.");

        if (revision.WorkflowStatus != "Draft")
            throw new CustomValidationException("WorkflowStatus", "Chỉ bản nháp (Draft) mới được phép gửi duyệt.");

        if (revision.Version != ifMatchVersion)
            throw new ConcurrencyException("Dữ liệu hồ sơ đã được cập nhật bởi người khác. Vui lòng tải lại.");

        // Strict validations on submit
        if (!revision.ReleaseId.HasValue)
            throw new CustomValidationException("ReleaseId", "Bắt buộc phải chọn phiên bản phát hành khi gửi duyệt.");

        var releaseBelongs = await _context.SoftwareReleases
            .AnyAsync(r => r.Id == revision.ReleaseId.Value && r.SoftwareId == revision.Deployment.SoftwareId, cancellationToken);
        if (!releaseBelongs)
            throw new CustomValidationException("ReleaseId", "Phiên bản phát hành không thuộc phần mềm của hồ sơ triển khai.");

        if (!revision.ResponsibleUserId.HasValue)
            throw new CustomValidationException("ResponsibleUserId", "Bắt buộc phải phân công cán bộ phụ trách khi gửi duyệt.");

        ValidateDates(revision.OperationalStatus, revision.StartDate, revision.GoLiveDate);

        var nowUtc = _dateTimeProvider.UtcNow;
        revision.WorkflowStatus = "Submitted";
        revision.SubmittedBy = currentUserId;
        revision.SubmittedAt = nowUtc;
        revision.UpdatedAt = nowUtc;
        revision.UpdatedBy = currentUserId;

        // Enqueue background job for workflow notification
        await _jobService.EnqueueAsync("WorkflowNotification", new
        {
            Action = "Submitted",
            RevisionId = revision.Id,
            DeploymentId = revision.DeploymentId,
            OrganizationId = revision.Deployment.OrganizationId,
            SoftwareName = revision.Deployment.Software.Name,
            SubmittedByUserId = currentUserId,
            Timestamp = nowUtc
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRevisionByIdAsync(revisionId, currentUserId, cancellationToken);
    }

    public async Task<DeploymentRevisionDto> ApproveRevisionAsync(
        Guid revisionId,
        long ifMatchVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var revision = await _context.DeploymentRevisions
            .Include(r => r.Deployment)
                .ThenInclude(d => d.Software)
            .FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);

        if (revision == null)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        var hasApprove = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.approve", revision.Deployment.OrganizationId, cancellationToken);
        if (!hasApprove)
            throw new ForbiddenException("Bạn không có quyền phê duyệt hồ sơ triển khai tại đơn vị này.");

        if (revision.WorkflowStatus != "Submitted")
            throw new CustomValidationException("WorkflowStatus", "Chỉ hồ sơ ở trạng thái chờ duyệt ('Submitted') mới được phép phê duyệt.");

        if (revision.Version != ifMatchVersion)
            throw new ConcurrencyException("Dữ liệu hồ sơ đã được cập nhật bởi người khác. Vui lòng tải lại.");

        // Anti-self-approval rule
        if (revision.SubmittedBy.HasValue && revision.SubmittedBy.Value == currentUserId)
        {
            throw new ForbiddenException("Người gửi duyệt không được tự phê duyệt hồ sơ của chính mình theo quy định.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;

        var decision = new ApprovalDecision
        {
            Id = Guid.NewGuid(),
            DeploymentRevisionId = revisionId,
            Decision = "Approved",
            Reason = null,
            ActorId = currentUserId,
            DecidedAt = nowUtc
        };

        revision.WorkflowStatus = "Approved";
        revision.ApprovedAt = nowUtc;
        revision.UpdatedAt = nowUtc;
        revision.UpdatedBy = currentUserId;

        var deployment = revision.Deployment;
        deployment.CurrentApprovedRevisionId = revisionId;
        deployment.UpdatedAt = nowUtc;
        deployment.UpdatedBy = currentUserId;

        _context.ApprovalDecisions.Add(decision);

        // Enqueue background job for notification
        await _jobService.EnqueueAsync("WorkflowNotification", new
        {
            Action = "Approved",
            RevisionId = revision.Id,
            DeploymentId = revision.DeploymentId,
            OrganizationId = deployment.OrganizationId,
            SoftwareName = deployment.Software.Name,
            SubmittedByUserId = revision.SubmittedBy,
            ActorId = currentUserId,
            Timestamp = nowUtc
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRevisionByIdAsync(revisionId, currentUserId, cancellationToken);
    }

    public async Task<DeploymentRevisionDto> RejectRevisionAsync(
        Guid revisionId,
        RejectRevisionDto dto,
        long ifMatchVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new CustomValidationException("Reason", "Lý do từ chối hồ sơ là bắt buộc.");

        if (dto.Reason.Trim().Length > 2000)
            throw new CustomValidationException("Reason", "Lý do từ chối không được vượt quá 2000 ký tự.");

        var revision = await _context.DeploymentRevisions
            .Include(r => r.Deployment)
                .ThenInclude(d => d.Software)
            .FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);

        if (revision == null)
            throw new NotFoundException("Bản sửa hồ sơ triển khai", revisionId);

        var hasApprove = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.approve", revision.Deployment.OrganizationId, cancellationToken);
        if (!hasApprove)
            throw new ForbiddenException("Bạn không có quyền từ chối hồ sơ triển khai tại đơn vị này.");

        if (revision.WorkflowStatus != "Submitted")
            throw new CustomValidationException("WorkflowStatus", "Chỉ hồ sơ ở trạng thái chờ duyệt ('Submitted') mới được phép từ chối.");

        if (revision.Version != ifMatchVersion)
            throw new ConcurrencyException("Dữ liệu hồ sơ đã được cập nhật bởi người khác. Vui lòng tải lại.");

        // Anti-self-approval rule check
        if (revision.SubmittedBy.HasValue && revision.SubmittedBy.Value == currentUserId)
        {
            throw new ForbiddenException("Người gửi duyệt không được tự thao tác trên hồ sơ của chính mình theo quy định.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var reasonTrimmed = dto.Reason.Trim();

        var decision = new ApprovalDecision
        {
            Id = Guid.NewGuid(),
            DeploymentRevisionId = revisionId,
            Decision = "Rejected",
            Reason = reasonTrimmed,
            ActorId = currentUserId,
            DecidedAt = nowUtc
        };

        revision.WorkflowStatus = "Rejected";
        revision.UpdatedAt = nowUtc;
        revision.UpdatedBy = currentUserId;

        _context.ApprovalDecisions.Add(decision);

        // Enqueue background job for notification
        await _jobService.EnqueueAsync("WorkflowNotification", new
        {
            Action = "Rejected",
            RevisionId = revision.Id,
            DeploymentId = revision.DeploymentId,
            OrganizationId = revision.Deployment.OrganizationId,
            SoftwareName = revision.Deployment.Software.Name,
            SubmittedByUserId = revision.SubmittedBy,
            ActorId = currentUserId,
            Reason = reasonTrimmed,
            Timestamp = nowUtc
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRevisionByIdAsync(revisionId, currentUserId, cancellationToken);
    }

    private static void ValidateDates(string operationalStatus, DateOnly? startDate, DateOnly? goLiveDate)
    {
        if (operationalStatus == "Active" && !goLiveDate.HasValue)
        {
            throw new CustomValidationException("GoLiveDate", "Ngày đưa vào sử dụng là bắt buộc khi trạng thái là 'Đang sử dụng' (Active).");
        }

        if (startDate.HasValue && goLiveDate.HasValue && goLiveDate.Value < startDate.Value)
        {
            throw new CustomValidationException("GoLiveDate", "Ngày đưa vào sử dụng không được trước ngày tiếp nhận phần mềm.");
        }
    }

    private static DeploymentDto MapToDeploymentDto(Deployment d, DeploymentRevision? activeRev)
    {
        var approvedDto = d.CurrentApprovedRevision != null ? MapToRevisionDto(d.CurrentApprovedRevision) : null;
        var activeDto = activeRev != null ? MapToRevisionDto(activeRev) : null;

        return new DeploymentDto(
            d.Id,
            d.SoftwareId,
            d.Software.Code,
            d.Software.Name,
            d.OrganizationId,
            d.Organization.Code,
            d.Organization.Versions.OrderByDescending(v => v.ValidFrom).FirstOrDefault()?.Name ?? d.Organization.Code,
            d.Environment,
            d.InstanceKey,
            d.CurrentApprovedRevisionId,
            approvedDto,
            activeDto,
            d.Version,
            d.CreatedAt,
            d.UpdatedAt);
    }

    private static DeploymentRevisionDto MapToRevisionDto(DeploymentRevision r)
    {
        var decisions = r.Decisions
            .OrderByDescending(dec => dec.DecidedAt)
            .Select(dec => new ApprovalDecisionDto(
                dec.Id,
                dec.DeploymentRevisionId,
                dec.Decision,
                dec.Reason,
                dec.ActorId,
                dec.Actor?.UserName ?? string.Empty,
                dec.Actor?.DisplayName ?? string.Empty,
                dec.DecidedAt))
            .ToList();

        return new DeploymentRevisionDto(
            r.Id,
            r.DeploymentId,
            r.RevisionNo,
            r.ReleaseId,
            r.Release?.VersionName,
            r.OperationalStatus,
            r.StartDate,
            r.GoLiveDate,
            r.ResponsibleUserId,
            r.ResponsibleUser?.UserName,
            r.ResponsibleUser?.DisplayName,
            r.WorkflowStatus,
            r.SubmittedBy,
            r.SubmittedByUser?.UserName,
            r.SubmittedByUser?.DisplayName,
            r.SubmittedAt,
            r.ApprovedAt,
            r.Version,
            r.CreatedAt,
            r.UpdatedAt,
            decisions);
    }
}
