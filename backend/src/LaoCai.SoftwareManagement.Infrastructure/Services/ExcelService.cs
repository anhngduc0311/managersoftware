using System.Security.Cryptography;
using System.Text.Json;
using ClosedXML.Excel;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Deployments;
using LaoCai.SoftwareManagement.Domain.Entities.Documents;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using LaoCai.SoftwareManagement.Domain.Entities.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Infrastructure.Services;

public class ExcelService : IExcelService
{
    private readonly IAppDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IBackgroundJobService _jobService;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileScanner _fileScanner;
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(
        IAppDbContext context,
        IFileStorage fileStorage,
        IBackgroundJobService jobService,
        IScopeAuthorizationService scopeAuth,
        IDateTimeProvider dateTimeProvider,
        IFileScanner fileScanner,
        ILogger<ExcelService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _jobService = jobService;
        _scopeAuth = scopeAuth;
        _dateTimeProvider = dateTimeProvider;
        _fileScanner = fileScanner;
        _logger = logger;
    }

    public async Task<byte[]> GenerateDeploymentTemplateAsync(CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();

        // 1. Data Entry Sheet
        var wsData = workbook.Worksheets.Add("Mau_Nhap_Lieu");

        var headers = new[]
        {
            "Mã phần mềm (*)",
            "Mã đơn vị (*)",
            "Phiên bản (*)",
            "Môi trường",
            "Ngày golive (yyyy-MM-dd)",
            "Cán bộ phụ trách",
            "Số điện thoại",
            "Email liên hệ",
            "Mã bản quyền",
            "Ghi chú"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = wsData.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E88E5");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Example row 2
        var exampleValues = new[]
        {
            "IOFFICE_LC",
            "VP_UBND_TINH",
            "4.5.1",
            "Production",
            "2024-01-15",
            "Nguyễn Văn A",
            "0912345678",
            "nva@laocai.gov.vn",
            "LIC-IOFF-2024-001",
            "Triển khai chính thức toàn văn phòng"
        };

        for (int i = 0; i < exampleValues.Length; i++)
        {
            wsData.Cell(2, i + 1).Value = exampleValues[i];
            wsData.Cell(2, i + 1).Style.Font.Italic = true;
            wsData.Cell(2, i + 1).Style.Font.FontColor = XLColor.DarkGray;
        }

        wsData.Columns().AdjustToContents();

        // 2. Reference Sheet: Softwares
        var wsSoftware = workbook.Worksheets.Add("DM_Phan_Mem");
        wsSoftware.Cell(1, 1).Value = "Mã phần mềm";
        wsSoftware.Cell(1, 2).Value = "Tên phần mềm";
        wsSoftware.Cell(1, 3).Value = "Nhà cung cấp";
        wsSoftware.Row(1).Style.Font.Bold = true;
        wsSoftware.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
        wsSoftware.Row(1).Style.Font.FontColor = XLColor.White;

        var softwares = await _context.Software
            .AsNoTracking()
            .Include(s => s.Vendor)
            .Where(s => s.LifecycleStatus == "Active")
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < softwares.Count; i++)
        {
            var sw = softwares[i];
            wsSoftware.Cell(i + 2, 1).Value = EscapeFormula(sw.Code);
            wsSoftware.Cell(i + 2, 2).Value = EscapeFormula(sw.Name);
            wsSoftware.Cell(i + 2, 3).Value = EscapeFormula(sw.Vendor?.Name ?? "");
        }
        wsSoftware.Columns().AdjustToContents();

        // 3. Reference Sheet: Organizations
        var wsOrgs = workbook.Worksheets.Add("DM_Don_Vi");
        wsOrgs.Cell(1, 1).Value = "Mã đơn vị";
        wsOrgs.Cell(1, 2).Value = "Tên đơn vị";
        wsOrgs.Cell(1, 3).Value = "Cấp hành chính";
        wsOrgs.Row(1).Style.Font.Bold = true;
        wsOrgs.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EF6C00");
        wsOrgs.Row(1).Style.Font.FontColor = XLColor.White;

        var orgs = await _context.Organizations
            .AsNoTracking()
            .Include(o => o.Versions)
            .Where(o => o.IsActive)
            .OrderBy(o => o.Code)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < orgs.Count; i++)
        {
            var org = orgs[i];
            var orgName = org.Versions.OrderByDescending(v => v.ValidFrom).Select(v => v.Name).FirstOrDefault() ?? org.Code;
            wsOrgs.Cell(i + 2, 1).Value = EscapeFormula(org.Code);
            wsOrgs.Cell(i + 2, 2).Value = EscapeFormula(orgName);
            wsOrgs.Cell(i + 2, 3).Value = EscapeFormula(org.Code);
        }
        wsOrgs.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<ImportBatchDto> UploadImportBatchAsync(
        Guid userId,
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            throw new CustomValidationException("file", "Chỉ chấp nhận tệp định dạng Excel (.xlsx).");
        }

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        var sizeBytes = memoryStream.Length;

        if (sizeBytes <= 0)
        {
            throw new CustomValidationException("file", "Tệp tải lên không được rỗng.");
        }

        if (sizeBytes > 20 * 1024 * 1024)
        {
            throw new CustomValidationException("file", "Dung lượng tệp vượt quá giới hạn 20 MB.");
        }

        memoryStream.Position = 0;
        string checksumSha256;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(memoryStream);
            checksumSha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        memoryStream.Position = 0;
        var storageKey = await _fileStorage.SaveQuarantineAsync(memoryStream, fileName, cancellationToken);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            StorageKey = storageKey,
            OriginalName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            SizeBytes = sizeBytes,
            ChecksumSha256 = checksumSha256,
            ScanStatus = "Pending",
            UploadedByUserId = userId,
            CreatedAt = _dateTimeProvider.UtcNow
        };
        _context.Documents.Add(document);

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Status = "Pending",
            RequestedByUserId = userId,
            TotalRows = 0,
            ValidRows = 0,
            ErrorRows = 0,
            CreatedAt = _dateTimeProvider.UtcNow
        };
        _context.ImportBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        // Enqueue background scanning and validation
        await _jobService.EnqueueAsync("document.scan", new { DocumentId = document.Id }, cancellationToken);
        await _jobService.EnqueueAsync("deployment.import.validate", new { BatchId = batch.Id }, cancellationToken);

        return new ImportBatchDto(
            batch.Id,
            batch.RequestedByUserId,
            document.OriginalName,
            document.StorageKey,
            batch.Status,
            batch.TotalRows,
            batch.ValidRows,
            batch.ErrorRows,
            batch.CreatedAt,
            batch.ValidatedAt,
            batch.CommittedAt,
            batch.ErrorMessage);
    }

    public async Task<ImportBatchDto> GetImportBatchAsync(
        Guid batchId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _context.ImportBatches
            .AsNoTracking()
            .Include(b => b.RowErrors)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        if (batch == null)
        {
            throw new NotFoundException("Lô nhập liệu Excel", batchId);
        }

        var doc = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == batch.DocumentId, cancellationToken);

        var errors = await _context.ImportRowErrors.AsNoTracking()
            .Where(e => e.BatchId == batchId)
            .OrderBy(e => e.RowIndex)
            .Select(e => new ImportRowErrorDto(e.RowIndex, e.ColumnName, e.ErrorMessage, e.RawValue, e.ErrorCode))
            .ToListAsync(cancellationToken);

        return new ImportBatchDto(
            batch.Id,
            batch.RequestedByUserId,
            doc?.OriginalName ?? "import.xlsx",
            doc?.StorageKey ?? "",
            batch.Status,
            batch.TotalRows,
            batch.ValidRows,
            batch.ErrorRows,
            batch.CreatedAt,
            batch.ValidatedAt,
            batch.CommittedAt,
            batch.ErrorMessage,
            errors);
    }

    public async Task<bool> ValidateImportBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.ImportBatches
            .Include(b => b.RowErrors)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        if (batch == null)
            return false;

        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == batch.DocumentId, cancellationToken);
        if (document == null)
            return false;

        batch.Status = "Validating";
        await _context.SaveChangesAsync(cancellationToken);

        // 1. Ensure file is scanned and Clean
        if (document.ScanStatus != "Clean")
        {
            try
            {
                ScanResult scanResult;
                await using (var qStream = await _fileStorage.OpenReadQuarantineAsync(document.StorageKey, cancellationToken))
                {
                    scanResult = await _fileScanner.ScanAsync(qStream, document.OriginalName, document.ContentType, cancellationToken);
                }

                if (scanResult.IsClean)
                {
                    await _fileStorage.MoveToCleanAsync(document.StorageKey, cancellationToken);
                    document.ScanStatus = "Clean";
                    document.CleanedAt = _dateTimeProvider.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    document.ScanStatus = "Rejected";
                    document.ScanMessage = scanResult.Message;
                    batch.Status = "FailedValidation";
                    batch.ErrorMessage = $"Tệp không an toàn (virus/mã độc): {scanResult.Message}";
                    await _context.SaveChangesAsync(cancellationToken);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi quét virus tệp batch {BatchId}", batchId);
                batch.Status = "FailedValidation";
                batch.ErrorMessage = "Lỗi khi kiểm tra an toàn tệp: " + ex.Message;
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }
        }

        // 2. Parse Excel
        Stream cleanStream;
        try
        {
            cleanStream = await _fileStorage.OpenReadCleanAsync(document.StorageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể mở tệp sạch cho batch {BatchId}", batchId);
            batch.Status = "FailedValidation";
            batch.ErrorMessage = "Không thể mở tệp sạch: " + ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }

        using (cleanStream)
        using (var workbook = new XLWorkbook(cleanStream))
        {
            var ws = workbook.Worksheet("Mau_Nhap_Lieu") ?? workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                batch.Status = "FailedValidation";
                batch.ErrorMessage = "Tệp Excel không chứa sheet dữ liệu hợp lệ.";
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }

            // Remove existing row errors
            _context.ImportRowErrors.RemoveRange(batch.RowErrors);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var dataRowCount = lastRow - 1;

            if (dataRowCount > 5000)
            {
                var rowErr = new ImportRowError
                {
                    Id = Guid.NewGuid(),
                    BatchId = batch.Id,
                    RowIndex = 1,
                    ColumnName = "Tệp",
                    ErrorCode = "MAX_ROWS_EXCEEDED",
                    ErrorMessage = $"Số lượng dòng dữ liệu ({dataRowCount:N0}) vượt quá giới hạn tối đa 5.000 dòng.",
                    RawValue = dataRowCount.ToString()
                };
                _context.ImportRowErrors.Add(rowErr);
                batch.Status = "FailedValidation";
                batch.TotalRows = dataRowCount;
                batch.ErrorRows = 1;
                batch.ValidRows = 0;
                batch.ErrorMessage = "Số lượng dòng trong tệp vượt quá giới hạn 5.000 dòng.";
                batch.ValidatedAt = _dateTimeProvider.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }

            // Reference lookups
            var activeSoftwares = await _context.Software.AsNoTracking()
                .Where(s => s.LifecycleStatus == "Active")
                .ToDictionaryAsync(s => s.Code.Trim().ToUpperInvariant(), s => s, cancellationToken);

            var activeOrgs = await _context.Organizations.AsNoTracking()
                .Include(o => o.Versions)
                .Where(o => o.IsActive)
                .ToDictionaryAsync(o => o.Code.Trim().ToUpperInvariant(), o => o, cancellationToken);

            var allowedOrgs = await _scopeAuth.GetAllowedOrganizationIdsAsync(batch.RequestedByUserId, "deployments.write", cancellationToken);

            var rowErrors = new List<ImportRowError>();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int validRowsCount = 0;
            int totalProcessed = 0;

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (row.IsEmpty())
                    continue;

                totalProcessed++;
                bool rowHasError = false;

                // Check Formula Injection on all cells in row
                foreach (var cell in row.CellsUsed())
                {
                    if (cell.HasFormula)
                    {
                        rowErrors.Add(new ImportRowError
                        {
                            Id = Guid.NewGuid(),
                            BatchId = batch.Id,
                            RowIndex = r,
                            ColumnName = cell.Address.ColumnLetter,
                            ErrorCode = "FORMULA_INJECTION",
                            ErrorMessage = $"Ô {cell.Address} chứa công thức (Formula: ={cell.FormulaA1}) có thể dẫn tới tấn công Formula Injection.",
                            RawValue = "=" + cell.FormulaA1
                        });
                        rowHasError = true;
                    }
                    else
                    {
                        var textVal = cell.GetString()?.Trim();
                        if (IsFormulaInjection(textVal))
                        {
                            rowErrors.Add(new ImportRowError
                            {
                                Id = Guid.NewGuid(),
                                BatchId = batch.Id,
                                RowIndex = r,
                                ColumnName = cell.Address.ColumnLetter,
                                ErrorCode = "FORMULA_INJECTION",
                                ErrorMessage = $"Ô {cell.Address} chứa ký tự bắt đầu nguy hiểm (=, +, -, @) có thể dẫn tới tấn công Formula Injection.",
                                RawValue = textVal
                            });
                            rowHasError = true;
                        }
                    }
                }

                var swCode = ws.Cell(r, 1).GetString()?.Trim() ?? "";
                var orgCode = ws.Cell(r, 2).GetString()?.Trim() ?? "";
                var version = ws.Cell(r, 3).GetString()?.Trim() ?? "";
                var env = ws.Cell(r, 4).GetString()?.Trim() ?? "Production";
                var goLiveStr = ws.Cell(r, 5).GetString()?.Trim() ?? "";

                // SoftwareCode check
                if (string.IsNullOrWhiteSpace(swCode))
                {
                    rowErrors.Add(new ImportRowError
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        RowIndex = r,
                        ColumnName = "Mã phần mềm",
                        ErrorCode = "REQUIRED_FIELD_MISSING",
                        ErrorMessage = "Mã phần mềm không được để trống.",
                        RawValue = swCode
                    });
                    rowHasError = true;
                }
                else if (!activeSoftwares.ContainsKey(swCode.ToUpperInvariant()))
                {
                    rowErrors.Add(new ImportRowError
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        RowIndex = r,
                        ColumnName = "Mã phần mềm",
                        ErrorCode = "SOFTWARE_NOT_FOUND",
                        ErrorMessage = $"Mã phần mềm '{swCode}' không tồn tại trong danh mục phần mềm đang hoạt động.",
                        RawValue = swCode
                    });
                    rowHasError = true;
                }

                // OrganizationCode check
                if (string.IsNullOrWhiteSpace(orgCode))
                {
                    rowErrors.Add(new ImportRowError
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        RowIndex = r,
                        ColumnName = "Mã đơn vị",
                        ErrorCode = "REQUIRED_FIELD_MISSING",
                        ErrorMessage = "Mã đơn vị không được để trống.",
                        RawValue = orgCode
                    });
                    rowHasError = true;
                }
                else if (!activeOrgs.TryGetValue(orgCode.ToUpperInvariant(), out var targetOrg))
                {
                    rowErrors.Add(new ImportRowError
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        RowIndex = r,
                        ColumnName = "Mã đơn vị",
                        ErrorCode = "ORGANIZATION_NOT_FOUND",
                        ErrorMessage = $"Mã đơn vị '{orgCode}' không tồn tại trong hệ thống.",
                        RawValue = orgCode
                    });
                    rowHasError = true;
                }
                else
                {
                    // Scope Authorization check
                    if (allowedOrgs != null && !allowedOrgs.Contains(targetOrg.Id))
                    {
                        var targetOrgName = targetOrg.Versions.OrderByDescending(v => v.ValidFrom).Select(v => v.Name).FirstOrDefault() ?? targetOrg.Code;
                        rowErrors.Add(new ImportRowError
                        {
                            Id = Guid.NewGuid(),
                            BatchId = batch.Id,
                            RowIndex = r,
                            ColumnName = "Mã đơn vị",
                            ErrorCode = "SCOPE_FORBIDDEN",
                            ErrorMessage = $"Người dùng không có quyền quản lý triển khai cho đơn vị '{targetOrgName}' ({targetOrg.Code}).",
                            RawValue = orgCode
                        });
                        rowHasError = true;
                    }
                }

                // Version check
                if (string.IsNullOrWhiteSpace(version))
                {
                    rowErrors.Add(new ImportRowError
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        RowIndex = r,
                        ColumnName = "Phiên bản",
                        ErrorCode = "REQUIRED_FIELD_MISSING",
                        ErrorMessage = "Phiên bản phần mềm không được để trống.",
                        RawValue = version
                    });
                    rowHasError = true;
                }

                // GoLiveDate format check
                if (!string.IsNullOrWhiteSpace(goLiveStr))
                {
                    if (!DateOnly.TryParse(goLiveStr, out _))
                    {
                        rowErrors.Add(new ImportRowError
                        {
                            Id = Guid.NewGuid(),
                            BatchId = batch.Id,
                            RowIndex = r,
                            ColumnName = "Ngày golive",
                            ErrorCode = "INVALID_DATE_FORMAT",
                            ErrorMessage = "Ngày golive không đúng định dạng (chuẩn yyyy-MM-dd).",
                            RawValue = goLiveStr
                        });
                        rowHasError = true;
                    }
                }

                // Duplicate within file check
                var dedupeKey = $"{swCode}:{orgCode}:{env}";
                if (seenKeys.Contains(dedupeKey))
                {
                    rowErrors.Add(new ImportRowError
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        RowIndex = r,
                        ColumnName = "Dòng dữ liệu",
                        ErrorCode = "DUPLICATE_ENTRY",
                        ErrorMessage = $"Bản ghi triển khai ({swCode} - {orgCode} - {env}) bị trùng lặp trong tệp nhập liệu.",
                        RawValue = dedupeKey
                    });
                    rowHasError = true;
                }
                else
                {
                    seenKeys.Add(dedupeKey);
                }

                if (!rowHasError)
                {
                    validRowsCount++;
                }
            }

            batch.TotalRows = totalProcessed;
            batch.ErrorRows = rowErrors.Count;
            batch.ValidRows = validRowsCount;
            batch.ValidatedAt = _dateTimeProvider.UtcNow;

            if (rowErrors.Count > 0)
            {
                batch.Status = "FailedValidation";
                batch.ErrorMessage = $"Phát hiện {rowErrors.Count} lỗi trong tệp nhập liệu. Vui lòng khắc phục toàn bộ lỗi trước khi commit.";
                _context.ImportRowErrors.AddRange(rowErrors);
            }
            else
            {
                batch.Status = "ReadyToCommit";
                batch.ErrorMessage = null;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return rowErrors.Count == 0;
        }
    }

    public async Task<int> CommitImportBatchAsync(
        Guid batchId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _context.ImportBatches
            .Include(b => b.RowErrors)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

        if (batch == null)
        {
            throw new NotFoundException("Lô nhập liệu", batchId);
        }

        if (batch.Status == "Committed")
        {
            return batch.ValidRows;
        }

        if (batch.Status != "ReadyToCommit" || batch.ErrorRows > 0)
        {
            throw new CustomValidationException("batch", "Không thể commit tệp nhập liệu khi còn lỗi hoặc chưa qua kiểm tra hợp lệ. Yêu cầu 0 lỗi (All-or-Nothing).");
        }

        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == batch.DocumentId, cancellationToken);
        if (document == null)
        {
            throw new CustomValidationException("document", "Không tìm thấy tệp tài liệu sạch tương ứng với lô nhập.");
        }

        batch.Status = "Committing";
        await _context.SaveChangesAsync(cancellationToken);

        using var cleanStream = await _fileStorage.OpenReadCleanAsync(document.StorageKey, cancellationToken);
        using var workbook = new XLWorkbook(cleanStream);
        var ws = workbook.Worksheet("Mau_Nhap_Lieu") ?? workbook.Worksheets.First();

        var activeSoftwares = await _context.Software
            .Where(s => s.LifecycleStatus == "Active")
            .ToDictionaryAsync(s => s.Code.Trim().ToUpperInvariant(), s => s, cancellationToken);

        var activeOrgs = await _context.Organizations
            .Where(o => o.IsActive)
            .ToDictionaryAsync(o => o.Code.Trim().ToUpperInvariant(), o => o, cancellationToken);

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        int importedCount = 0;

        for (int r = 2; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (row.IsEmpty())
                continue;

            var swCode = ws.Cell(r, 1).GetString()?.Trim().ToUpperInvariant() ?? "";
            var orgCode = ws.Cell(r, 2).GetString()?.Trim().ToUpperInvariant() ?? "";
            var version = ws.Cell(r, 3).GetString()?.Trim() ?? "";
            var env = ws.Cell(r, 4).GetString()?.Trim() ?? "Production";
            var goLiveStr = ws.Cell(r, 5).GetString()?.Trim() ?? "";
            var assignedPersonnel = ws.Cell(r, 6).GetString()?.Trim();
            var phone = ws.Cell(r, 7).GetString()?.Trim();
            var email = ws.Cell(r, 8).GetString()?.Trim();
            var licenseKey = ws.Cell(r, 9).GetString()?.Trim();
            var notes = ws.Cell(r, 10).GetString()?.Trim();

            if (!activeSoftwares.TryGetValue(swCode, out var sw) || !activeOrgs.TryGetValue(orgCode, out var org))
                continue;

            DateOnly? goLiveDate = DateOnly.TryParse(goLiveStr, out var gld) ? gld : null;

            // Find or create deployment
            var deployment = await _context.Deployments
                .Include(d => d.Revisions)
                .FirstOrDefaultAsync(d => d.SoftwareId == sw.Id && d.OrganizationId == org.Id && d.Environment == env, cancellationToken);

            if (deployment == null)
            {
                deployment = new Deployment
                {
                    Id = Guid.NewGuid(),
                    SoftwareId = sw.Id,
                    OrganizationId = org.Id,
                    Environment = env,
                    InstanceKey = "default",
                    Version = 1,
                    CreatedAt = _dateTimeProvider.UtcNow,
                    CreatedBy = userId
                };
                _context.Deployments.Add(deployment);
            }

            var revisionNo = (deployment.Revisions?.Count ?? 0) + 1;
            var revision = new DeploymentRevision
            {
                Id = Guid.NewGuid(),
                DeploymentId = deployment.Id,
                RevisionNo = revisionNo,
                OperationalStatus = "NotInUse",
                WorkflowStatus = "Draft",
                GoLiveDate = goLiveDate,
                Version = 1,
                CreatedAt = _dateTimeProvider.UtcNow,
                CreatedBy = userId
            };
            _context.DeploymentRevisions.Add(revision);

            importedCount++;
        }

        batch.Status = "Committed";
        batch.CommittedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return importedCount;
    }

    public async Task<ExportRequestDto> RequestExportAsync(
        Guid userId,
        ExportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var hasExportPerm = await _scopeAuth.HasPermissionAsync(userId, "reports.export", null, cancellationToken);
        var hasReadPerm = await _scopeAuth.HasPermissionAsync(userId, "deployments.read", null, cancellationToken);

        if (!hasExportPerm && !hasReadPerm)
        {
            throw new ForbiddenException("Bạn không có quyền yêu cầu xuất dữ liệu báo cáo.");
        }

        var exportReq = new ExportRequest
        {
            Id = Guid.NewGuid(),
            ExportType = "Deployments",
            FilterSnapshotJson = JsonSerializer.Serialize(filter),
            Status = "Queued",
            RequestedByUserId = userId,
            ExpiresAt = _dateTimeProvider.UtcNow.AddHours(24),
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.ExportRequests.Add(exportReq);
        await _context.SaveChangesAsync(cancellationToken);

        await _jobService.EnqueueAsync("deployment.export", new { ExportRequestId = exportReq.Id }, cancellationToken);

        return new ExportRequestDto(
            exportReq.Id,
            exportReq.RequestedByUserId,
            exportReq.Status,
            null,
            null,
            null,
            null,
            exportReq.CreatedAt,
            exportReq.CompletedAt,
            exportReq.ExpiresAt,
            exportReq.ErrorMessage);
    }

    public async Task<ExportRequestDto> GetExportRequestAsync(
        Guid exportId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var exportReq = await _context.ExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exportId, cancellationToken);

        if (exportReq == null)
        {
            throw new NotFoundException("Yêu cầu xuất dữ liệu", exportId);
        }

        Document? doc = null;
        if (exportReq.DocumentId.HasValue)
        {
            doc = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == exportReq.DocumentId.Value, cancellationToken);
        }

        return new ExportRequestDto(
            exportReq.Id,
            exportReq.RequestedByUserId,
            exportReq.Status,
            doc?.StorageKey,
            doc?.OriginalName,
            doc?.SizeBytes,
            null,
            exportReq.CreatedAt,
            exportReq.CompletedAt,
            exportReq.ExpiresAt,
            exportReq.ErrorMessage);
    }

    public async Task ProcessExportAsync(Guid exportId, CancellationToken cancellationToken = default)
    {
        var exportReq = await _context.ExportRequests.FirstOrDefaultAsync(e => e.Id == exportId, cancellationToken);
        if (exportReq == null || exportReq.Status != "Queued")
            return;

        exportReq.Status = "Processing";
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var filter = JsonSerializer.Deserialize<ExportFilterDto>(exportReq.FilterSnapshotJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ExportFilterDto(null, null, null, null);

            var allowedOrgs = await _scopeAuth.GetAllowedOrganizationIdsAsync(exportReq.RequestedByUserId, "deployments.read", cancellationToken);
            var canReadContracts = await _scopeAuth.HasPermissionAsync(exportReq.RequestedByUserId, "contracts.read", null, cancellationToken);

            var query = _context.Deployments
                .AsNoTracking()
                .Include(d => d.Software)
                .Include(d => d.Organization)
                    .ThenInclude(o => o.Versions)
                .Include(d => d.CurrentApprovedRevision)
                .AsQueryable();

            if (allowedOrgs != null)
            {
                query = query.Where(d => allowedOrgs.Contains(d.OrganizationId));
            }

            if (filter.SoftwareId.HasValue)
            {
                query = query.Where(d => d.SoftwareId == filter.SoftwareId.Value);
            }

            if (filter.OrganizationId.HasValue)
            {
                query = query.Where(d => d.OrganizationId == filter.OrganizationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(d => d.CurrentApprovedRevision != null && d.CurrentApprovedRevision.OperationalStatus == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(d => d.Software.Name.ToLower().Contains(term) || d.Organization.Code.ToLower().Contains(term));
            }

            var rawDeployments = await query.ToListAsync(cancellationToken);
            var deployments = rawDeployments
                .OrderBy(d => d.Organization.Versions.OrderByDescending(v => v.ValidFrom).Select(v => v.Name).FirstOrDefault() ?? d.Organization.Code)
                .ThenBy(d => d.Software.Name)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh_Sach_Trien_Khai");

            var headers = new List<string>
            {
                "STT",
                "Mã đơn vị",
                "Tên đơn vị",
                "Mã phần mềm",
                "Tên phần mềm",
                "Môi trường",
                "Trạng thái vận hành",
                "Trạng thái phê duyệt",
                "Ngày Go-live"
            };

            if (canReadContracts)
            {
                headers.Add("Hợp đồng liên quan");
            }

            for (int i = 0; i < headers.Count; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
                cell.Style.Font.FontColor = XLColor.White;
            }

            for (int i = 0; i < deployments.Count; i++)
            {
                var dep = deployments[i];
                var depOrgName = dep.Organization.Versions.OrderByDescending(v => v.ValidFrom).Select(v => v.Name).FirstOrDefault() ?? dep.Organization.Code;
                int col = 1;
                ws.Cell(i + 2, col++).Value = i + 1;
                ws.Cell(i + 2, col++).Value = EscapeFormula(dep.Organization.Code);
                ws.Cell(i + 2, col++).Value = EscapeFormula(depOrgName);
                ws.Cell(i + 2, col++).Value = EscapeFormula(dep.Software.Code);
                ws.Cell(i + 2, col++).Value = EscapeFormula(dep.Software.Name);
                ws.Cell(i + 2, col++).Value = EscapeFormula(dep.Environment);
                ws.Cell(i + 2, col++).Value = EscapeFormula(dep.CurrentApprovedRevision?.OperationalStatus ?? "NotInUse");
                ws.Cell(i + 2, col++).Value = EscapeFormula(dep.CurrentApprovedRevision?.WorkflowStatus ?? "Draft");
                ws.Cell(i + 2, col++).Value = dep.CurrentApprovedRevision?.GoLiveDate.HasValue == true
                    ? dep.CurrentApprovedRevision.GoLiveDate.Value.ToString("yyyy-MM-dd")
                    : "";

                if (canReadContracts)
                {
                    ws.Cell(i + 2, col++).Value = "Theo phân bổ bản quyền";
                }
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var fileBytes = ms.ToArray();

            var fileName = $"BaoCao_TrienKhai_{_dateTimeProvider.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            using var fileUploadStream = new MemoryStream(fileBytes);
            var storageKey = await _fileStorage.SaveQuarantineAsync(fileUploadStream, fileName, cancellationToken);
            await _fileStorage.MoveToCleanAsync(storageKey, cancellationToken);

            string checksumSha256;
            using (var sha = SHA256.Create())
            {
                checksumSha256 = Convert.ToHexString(sha.ComputeHash(fileBytes)).ToLowerInvariant();
            }

            var doc = new Document
            {
                Id = Guid.NewGuid(),
                StorageKey = storageKey,
                OriginalName = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                SizeBytes = fileBytes.Length,
                ChecksumSha256 = checksumSha256,
                ScanStatus = "Clean",
                CleanedAt = _dateTimeProvider.UtcNow,
                UploadedByUserId = exportReq.RequestedByUserId,
                CreatedAt = _dateTimeProvider.UtcNow
            };
            _context.Documents.Add(doc);

            exportReq.DocumentId = doc.Id;
            exportReq.Status = "Completed";
            exportReq.CompletedAt = _dateTimeProvider.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất dữ liệu báo cáo cho yêu cầu {ExportId}", exportId);
            exportReq.Status = "Failed";
            exportReq.ErrorMessage = "Lỗi tạo tệp xuất: " + ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> DownloadExportAsync(
        Guid exportId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var exportReq = await _context.ExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exportId, cancellationToken);

        if (exportReq == null)
        {
            throw new NotFoundException("Yêu cầu xuất dữ liệu", exportId);
        }

        // TTL check: 24 hours
        if (exportReq.ExpiresAt <= _dateTimeProvider.UtcNow)
        {
            throw new CustomValidationException("export", "Tệp xuất dữ liệu đã hết hạn (TTL 24 giờ). Vui lòng gửi yêu cầu xuất lại.");
        }

        if (exportReq.Status != "Completed" || !exportReq.DocumentId.HasValue)
        {
            throw new CustomValidationException("export", "Tệp xuất chưa sẵn sàng hoặc đã bị lỗi xử lý.");
        }

        // Scope check
        if (exportReq.RequestedByUserId != userId)
        {
            var hasExportPerm = await _scopeAuth.HasPermissionAsync(userId, "reports.export", null, cancellationToken);
            if (!hasExportPerm)
            {
                throw new ForbiddenException("Bạn không có quyền tải tệp xuất dữ liệu của người dùng khác.");
            }
        }

        var doc = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == exportReq.DocumentId.Value, cancellationToken);
        if (doc == null)
        {
            throw new NotFoundException("Tài liệu xuất dữ liệu", exportReq.DocumentId.Value);
        }

        var stream = await _fileStorage.OpenReadCleanAsync(doc.StorageKey, cancellationToken);
        return (stream, doc.OriginalName, doc.ContentType);
    }

    private static string EscapeFormula(string? val)
    {
        if (string.IsNullOrEmpty(val))
            return string.Empty;

        var trimmed = val.TrimStart();
        if (trimmed.Length > 0 && (trimmed[0] == '=' || trimmed[0] == '+' || trimmed[0] == '-' || trimmed[0] == '@'))
        {
            return "'" + val;
        }

        return val;
    }

    private static bool IsFormulaInjection(string? val)
    {
        if (string.IsNullOrWhiteSpace(val))
            return false;

        var trimmed = val.TrimStart();
        return trimmed.Length > 0 && (trimmed[0] == '=' || trimmed[0] == '+' || trimmed[0] == '-' || trimmed[0] == '@');
    }
}
