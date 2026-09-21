import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { SoftwareService, SoftwareDto, SoftwareCategoryDto, VendorDto } from '@core/services/software.service';
import { AuthService } from '@core/services/auth.service';
import { SoftwareDetailComponent } from './software-detail.component';

@Component({
  selector: 'app-software-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SoftwareDetailComponent, FormsModule, RouterModule],
  templateUrl: './software-list.component.html',
  styleUrls: ['./software-list.component.scss']
})
export class SoftwareListComponent implements OnInit {
  private softwareService = inject(SoftwareService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);

  activeTab = signal<'software' | 'categories' | 'vendors'>('software');
  softwareList = signal<SoftwareDto[]>([]);
  categories = signal<SoftwareCategoryDto[]>([]);
  vendors = signal<VendorDto[]>([]);
  isLoading = signal<boolean>(false);

  // Filters
  searchTerm = signal<string>('');
  selectedCategoryId = signal<string>('');
  selectedVendorId = signal<string>('');
  selectedLifecycle = signal<string>('');

  // Modals & Detail
  isCreateSoftwareModalOpen = signal<boolean>(false);
  isEditSoftwareModalOpen = signal<boolean>(false);
  isDeleteSoftwareModalOpen = signal<boolean>(false);
  softwareToEdit = signal<SoftwareDto | null>(null);
  softwareToDelete = signal<SoftwareDto | null>(null);

  isCreateCategoryModalOpen = signal<boolean>(false);
  isEditCategoryModalOpen = signal<boolean>(false);
  isDeleteCategoryModalOpen = signal<boolean>(false);
  categoryToEdit = signal<SoftwareCategoryDto | null>(null);
  categoryToDelete = signal<SoftwareCategoryDto | null>(null);

  isCreateVendorModalOpen = signal<boolean>(false);
  isEditVendorModalOpen = signal<boolean>(false);
  isDeleteVendorModalOpen = signal<boolean>(false);
  vendorToEdit = signal<VendorDto | null>(null);
  vendorToDelete = signal<VendorDto | null>(null);

  // Smart Form States
  isAutoCodeEnabled = signal<boolean>(true);
  isQuickCategoryOpen = signal<boolean>(false);
  isQuickVendorOpen = signal<boolean>(false);
  selectedPresetName = signal<string | null>(null);

  selectedSoftware = signal<SoftwareDto | null>(null);
  modalError = signal<string | null>(null);
  deleteError = signal<string | null>(null);
  toast = signal<{ type: 'success' | 'error'; message: string } | null>(null);

  // Smart Presets
  softwarePresets = [
    {
      label: 'VNPT iOffice',
      name: 'Hệ thống Quản lý văn bản và Điều hành',
      code: 'VNPT_IOFFICE',
      categoryKeyword: 'EGOV',
      vendorKeyword: 'VNPT',
      versionName: 'v4.5.0',
      description: 'Quản lý văn bản đi, đến, ký số điện tử và điều hành công việc trực tuyến toàn tỉnh'
    },
    {
      label: 'VNPT iGate',
      name: 'Cổng Dịch vụ công & Một cửa điện tử',
      code: 'VNPT_IGATE',
      categoryKeyword: 'EGOV',
      vendorKeyword: 'VNPT',
      versionName: 'v3.2.0',
      description: 'Tiếp nhận, số hóa và xử lý thủ tục hành chính công trực tuyến'
    },
    {
      label: 'Viettel HIS',
      name: 'Hệ thống Quản lý bệnh viện & Khám chữa bệnh (HIS)',
      code: 'VIETTEL_HIS',
      categoryKeyword: 'HEALTH',
      vendorKeyword: 'VIETTEL',
      versionName: 'v2.8.0',
      description: 'Quản lý thông tin bệnh nhân, bệnh án điện tử và viện phí BHYT'
    },
    {
      label: 'Viettel SMAS',
      name: 'Hệ thống Quản lý trường học trực tuyến (SMAS)',
      code: 'VIETTEL_SMAS',
      categoryKeyword: 'EDU',
      vendorKeyword: 'VIETTEL',
      versionName: 'v3.5.0',
      description: 'Sổ điểm điện tử, quản lý học sinh, giáo viên và liên thông Sở GD&ĐT'
    },
    {
      label: 'MISA Mimosa',
      name: 'Phần mềm Kế toán Hành chính sự nghiệp Mimosa',
      code: 'MISA_MIMOSA',
      categoryKeyword: 'FINANCE',
      vendorKeyword: 'MISA',
      versionName: 'v2026.1',
      description: 'Kế toán ngân sách nhà nước, quản lý chứng từ và báo cáo tài chính kho bạc'
    },
    {
      label: 'Hóa đơn điện tử',
      name: 'Hệ thống Quản lý Hóa đơn & Ký số điện tử',
      code: 'VNPT_EINVOICE',
      categoryKeyword: 'EGOV',
      vendorKeyword: 'VNPT',
      versionName: 'v2.0.0',
      description: 'Phát hành và quản lý hóa đơn điện tử có mã của cơ quan Thuế'
    }
  ];

  // Forms
  softwareForm = this.fb.group({
    code: ['', [Validators.required]],
    name: ['', [Validators.required]],
    categoryId: ['', [Validators.required]],
    vendorId: ['', [Validators.required]],
    description: [''],
    lifecycleStatus: ['Active', [Validators.required]],
    includeInitialRelease: [true],
    initialVersionName: ['v1.0.0'],
    initialReleaseDate: [new Date().toISOString().substring(0, 10)],
    initialSupportEndDate: ['']
  });

  quickCategoryForm = this.fb.group({
    name: ['', [Validators.required]],
    code: ['', [Validators.required]]
  });

  quickVendorForm = this.fb.group({
    name: ['', [Validators.required]],
    code: ['', [Validators.required]],
    contactInfo: ['']
  });

  editSoftwareForm = this.fb.group({
    code: [{ value: '', disabled: true }],
    name: ['', [Validators.required]],
    categoryId: ['', [Validators.required]],
    vendorId: ['', [Validators.required]],
    description: [''],
    lifecycleStatus: ['Active', [Validators.required]]
  });

  categoryForm = this.fb.group({
    code: ['', [Validators.required]],
    name: ['', [Validators.required]]
  });

  editCategoryForm = this.fb.group({
    code: [{ value: '', disabled: true }],
    name: ['', [Validators.required]],
    isActive: [true, [Validators.required]]
  });

  vendorForm = this.fb.group({
    code: ['', [Validators.required]],
    name: ['', [Validators.required]],
    contactInfo: ['']
  });

  editVendorForm = this.fb.group({
    code: [{ value: '', disabled: true }],
    name: ['', [Validators.required]],
    contactInfo: [''],
    isActive: [true, [Validators.required]]
  });

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const tab = params['tab'];
      if (tab === 'software' || tab === 'categories' || tab === 'vendors') {
        this.activeTab.set(tab);
      }
    });
    this.loadCatalogData();
  }

  showToast(type: 'success' | 'error', message: string) {
    this.toast.set({ type, message });
    setTimeout(() => {
      if (this.toast()?.message === message) {
        this.toast.set(null);
      }
    }, 4000);
  }

  setTab(tab: 'software' | 'categories' | 'vendors') {
    this.activeTab.set(tab);
  }

  loadCatalogData() {
    this.loadCategories();
    this.loadVendors();
    this.loadSoftware();
  }

  loadSoftware() {
    this.isLoading.set(true);
    this.softwareService.getSoftware({
      search: this.searchTerm(),
      categoryId: this.selectedCategoryId() || undefined,
      vendorId: this.selectedVendorId() || undefined,
      lifecycleStatus: this.selectedLifecycle() || undefined,
      pageSize: 50
    }).subscribe({
      next: (res) => {
        this.softwareList.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadCategories() {
    this.softwareService.getCategories().subscribe({
      next: (data) => this.categories.set(data)
    });
  }

  loadVendors() {
    this.softwareService.getVendors().subscribe({
      next: (data) => this.vendors.set(data)
    });
  }

  onFilterChange() {
    this.loadSoftware();
  }

  openSoftwareDetail(item: SoftwareDto) {
    this.selectedSoftware.set(item);
  }

  closeSoftwareDetail() {
    this.selectedSoftware.set(null);
  }

  // --- Smart Helpers ---

  removeVietnameseTones(str: string): string {
    return str
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'D')
      .replace(/[^a-zA-Z0-9\s_]/g, ' ')
      .trim();
  }

  generateCodeFromName(name: string, vendorId?: string): string {
    if (!name.trim()) return '';

    const clean = this.removeVietnameseTones(name).toUpperCase();
    const words = clean.split(/\s+/).filter(w => w.length > 0);
    
    let base = words.join('_');
    if (base.length > 30) {
      base = words.map(w => w[0]).join('');
      if (base.length < 3) {
        base = words.slice(0, 3).join('_');
      }
    }

    if (vendorId) {
      const vendor = this.vendors().find(v => v.id === vendorId);
      if (vendor) {
        const vendorPrefix = vendor.code.replace(/[^A-Z0-9]/gi, '_').toUpperCase();
        if (!base.startsWith(vendorPrefix)) {
          base = `${vendorPrefix}_${base}`;
        }
      }
    }

    return base.substring(0, 45);
  }

  onNameChange(name: string) {
    if (this.isAutoCodeEnabled()) {
      const vendorId = this.softwareForm.get('vendorId')?.value || '';
      const autoCode = this.generateCodeFromName(name, vendorId);
      this.softwareForm.patchValue({ code: autoCode });
    }
  }

  onVendorChange(vendorId: string) {
    if (this.isAutoCodeEnabled()) {
      const name = this.softwareForm.get('name')?.value || '';
      if (name) {
        const autoCode = this.generateCodeFromName(name, vendorId);
        this.softwareForm.patchValue({ code: autoCode });
      }
    }
  }

  regenerateCode() {
    const name = this.softwareForm.get('name')?.value || '';
    const vendorId = this.softwareForm.get('vendorId')?.value || '';
    const code = this.generateCodeFromName(name, vendorId);
    this.softwareForm.patchValue({ code });
  }

  applyPreset(preset: typeof this.softwarePresets[0]) {
    this.selectedPresetName.set(preset.label);

    // Find category matching keyword
    const cat = this.categories().find(c => 
      c.code.toUpperCase().includes(preset.categoryKeyword) ||
      c.name.toLowerCase().includes(preset.categoryKeyword.toLowerCase())
    ) || this.categories()[0];

    // Find vendor matching keyword
    const vendor = this.vendors().find(v => 
      v.code.toUpperCase().includes(preset.vendorKeyword) ||
      v.name.toLowerCase().includes(preset.vendorKeyword.toLowerCase())
    ) || this.vendors()[0];

    this.softwareForm.patchValue({
      name: preset.name,
      code: preset.code,
      categoryId: cat ? cat.id : '',
      vendorId: vendor ? vendor.id : '',
      description: preset.description,
      lifecycleStatus: 'Active',
      includeInitialRelease: true,
      initialVersionName: preset.versionName
    });

    this.isAutoCodeEnabled.set(false);
  }

  // --- Inline Quick Creation Handlers ---

  toggleQuickCategory() {
    this.isQuickCategoryOpen.update(v => !v);
    if (this.isQuickCategoryOpen()) {
      this.quickCategoryForm.reset({ name: '', code: '' });
    }
  }

  onQuickCategoryNameChange(name: string) {
    const code = this.removeVietnameseTones(name).replace(/\s+/g, '_').toUpperCase().substring(0, 20);
    this.quickCategoryForm.patchValue({ code });
  }

  submitQuickCategory() {
    if (this.quickCategoryForm.invalid) {
      this.quickCategoryForm.markAllAsTouched();
      return;
    }

    const val = this.quickCategoryForm.getRawValue();
    this.softwareService.createCategory({
      name: val.name!,
      code: val.code!
    }).subscribe({
      next: (newCat) => {
        this.categories.update(list => [...list, newCat]);
        this.softwareForm.patchValue({ categoryId: newCat.id });
        this.isQuickCategoryOpen.set(false);
        this.showToast('success', `Đã thêm nhóm "${newCat.name}" và tự động chọn!`);
      },
      error: (err) => {
        alert(err.problem?.detail || err.error?.detail || 'Không thể thêm nhóm mới.');
      }
    });
  }

  toggleQuickVendor() {
    this.isQuickVendorOpen.update(v => !v);
    if (this.isQuickVendorOpen()) {
      this.quickVendorForm.reset({ name: '', code: '', contactInfo: '' });
    }
  }

  onQuickVendorNameChange(name: string) {
    const code = this.removeVietnameseTones(name).replace(/\s+/g, '_').toUpperCase().substring(0, 25);
    this.quickVendorForm.patchValue({ code });
  }

  submitQuickVendor() {
    if (this.quickVendorForm.invalid) {
      this.quickVendorForm.markAllAsTouched();
      return;
    }

    const val = this.quickVendorForm.getRawValue();
    this.softwareService.createVendor({
      name: val.name!,
      code: val.code!,
      contactInfo: val.contactInfo || ''
    }).subscribe({
      next: (newVendor) => {
        this.vendors.update(list => [...list, newVendor]);
        this.softwareForm.patchValue({ vendorId: newVendor.id });
        this.onVendorChange(newVendor.id);
        this.isQuickVendorOpen.set(false);
        this.showToast('success', `Đã thêm NCC "${newVendor.name}" và tự động chọn!`);
      },
      error: (err) => {
        alert(err.problem?.detail || err.error?.detail || 'Không thể thêm nhà cung cấp mới.');
      }
    });
  }

  // --- Software Management (Thêm / Sửa / Xoá) ---

  // 1. Create Software
  openCreateSoftwareModal() {
    this.isAutoCodeEnabled.set(true);
    this.isQuickCategoryOpen.set(false);
    this.isQuickVendorOpen.set(false);
    this.selectedPresetName.set(null);

    const defaultCatId = this.categories().length > 0 ? this.categories()[0].id : '';
    const defaultVendorId = this.vendors().length > 0 ? this.vendors()[0].id : '';

    this.softwareForm.reset({
      code: '',
      name: '',
      categoryId: defaultCatId,
      vendorId: defaultVendorId,
      description: '',
      lifecycleStatus: 'Active',
      includeInitialRelease: true,
      initialVersionName: 'v1.0.0',
      initialReleaseDate: new Date().toISOString().substring(0, 10),
      initialSupportEndDate: ''
    });
    this.modalError.set(null);
    this.isCreateSoftwareModalOpen.set(true);
  }

  closeCreateSoftwareModal() {
    this.isCreateSoftwareModalOpen.set(false);
  }

  submitCreateSoftware() {
    if (this.softwareForm.invalid) {
      this.softwareForm.markAllAsTouched();
      return;
    }

    const val = this.softwareForm.getRawValue();
    this.modalError.set(null);

    this.softwareService.createSoftware({
      code: val.code!,
      name: val.name!,
      categoryId: val.categoryId!,
      vendorId: val.vendorId!,
      description: val.description || '',
      lifecycleStatus: val.lifecycleStatus!,
      initialVersionName: val.includeInitialRelease ? val.initialVersionName : null,
      initialReleaseDate: val.includeInitialRelease && val.initialReleaseDate ? new Date(val.initialReleaseDate).toISOString() : null,
      initialSupportEndDate: val.includeInitialRelease && val.initialSupportEndDate ? new Date(val.initialSupportEndDate).toISOString() : null
    }).subscribe({
      next: () => {
        this.closeCreateSoftwareModal();
        this.loadSoftware();
        this.showToast('success', 'Thêm phần mềm mới thành công!');
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo phần mềm.');
      }
    });
  }


  // 2. Edit Software
  openEditSoftwareModal(software: SoftwareDto, event?: Event) {
    event?.stopPropagation();
    this.softwareToEdit.set(software);
    this.editSoftwareForm.patchValue({
      code: software.code,
      name: software.name,
      categoryId: software.categoryId,
      vendorId: software.vendorId,
      description: software.description || '',
      lifecycleStatus: software.lifecycleStatus
    });
    this.modalError.set(null);
    this.isEditSoftwareModalOpen.set(true);
  }

  closeEditSoftwareModal() {
    this.isEditSoftwareModalOpen.set(false);
    this.softwareToEdit.set(null);
  }

  submitEditSoftware() {
    if (this.editSoftwareForm.invalid) {
      this.editSoftwareForm.markAllAsTouched();
      return;
    }

    const target = this.softwareToEdit();
    if (!target) return;

    const val = this.editSoftwareForm.getRawValue();
    this.modalError.set(null);

    this.softwareService.updateSoftware(target.id, {
      name: val.name!,
      categoryId: val.categoryId!,
      vendorId: val.vendorId!,
      description: val.description || '',
      lifecycleStatus: val.lifecycleStatus!
    }).subscribe({
      next: (updated) => {
        this.closeEditSoftwareModal();
        this.loadSoftware();
        // Update selectedSoftware if drawer is open
        if (this.selectedSoftware()?.id === target.id) {
          this.selectedSoftware.update(s => s ? { ...s, ...updated, categoryName: this.categories().find(c => c.id === updated.categoryId)?.name || s.categoryName, vendorName: this.vendors().find(v => v.id === updated.vendorId)?.name || s.vendorName } : null);
        }
        this.showToast('success', `Đã cập nhật thông tin phần mềm "${val.name}" thành công!`);
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể cập nhật phần mềm.');
      }
    });
  }

  // 3. Delete Software
  openDeleteSoftwareModal(software: SoftwareDto, event?: Event) {
    event?.stopPropagation();
    this.softwareToDelete.set(software);
    this.deleteError.set(null);
    this.isDeleteSoftwareModalOpen.set(true);
  }

  closeDeleteSoftwareModal() {
    this.isDeleteSoftwareModalOpen.set(false);
    this.softwareToDelete.set(null);
    this.deleteError.set(null);
  }

  submitDeleteSoftware() {
    const target = this.softwareToDelete();
    if (!target) return;

    this.deleteError.set(null);
    this.softwareService.deleteSoftware(target.id).subscribe({
      next: () => {
        if (this.selectedSoftware()?.id === target.id) {
          this.closeSoftwareDetail();
        }
        this.closeDeleteSoftwareModal();
        this.loadSoftware();
        this.showToast('success', `Đã xoá phần mềm "${target.name}" khỏi danh mục!`);
      },
      error: (err) => {
        this.deleteError.set(err.problem?.detail || err.error?.detail || 'Không thể xoá phần mềm này.');
      }
    });
  }

  // --- Category Management (Thêm / Sửa / Xoá) ---

  // 1. Create Category
  openCreateCategoryModal() {
    this.categoryForm.reset({ code: '', name: '' });
    this.modalError.set(null);
    this.isCreateCategoryModalOpen.set(true);
  }

  closeCreateCategoryModal() {
    this.isCreateCategoryModalOpen.set(false);
  }

  submitCreateCategory() {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    const val = this.categoryForm.getRawValue();
    this.modalError.set(null);

    this.softwareService.createCategory({
      code: val.code!,
      name: val.name!
    }).subscribe({
      next: () => {
        this.closeCreateCategoryModal();
        this.loadCategories();
        this.showToast('success', 'Thêm nhóm phần mềm thành công!');
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo nhóm phần mềm.');
      }
    });
  }

  // 2. Edit Category
  openEditCategoryModal(cat: SoftwareCategoryDto) {
    this.categoryToEdit.set(cat);
    this.editCategoryForm.patchValue({
      code: cat.code,
      name: cat.name,
      isActive: cat.isActive
    });
    this.modalError.set(null);
    this.isEditCategoryModalOpen.set(true);
  }

  closeEditCategoryModal() {
    this.isEditCategoryModalOpen.set(false);
    this.categoryToEdit.set(null);
  }

  submitEditCategory() {
    if (this.editCategoryForm.invalid) {
      this.editCategoryForm.markAllAsTouched();
      return;
    }

    const target = this.categoryToEdit();
    if (!target) return;

    const val = this.editCategoryForm.getRawValue();
    this.modalError.set(null);

    this.softwareService.updateCategory(target.id, {
      name: val.name!,
      isActive: val.isActive ?? true
    }).subscribe({
      next: () => {
        this.closeEditCategoryModal();
        this.loadCategories();
        this.showToast('success', `Đã cập nhật nhóm "${val.name}" thành công!`);
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể cập nhật nhóm phần mềm.');
      }
    });
  }

  // 3. Delete Category
  openDeleteCategoryModal(cat: SoftwareCategoryDto) {
    this.categoryToDelete.set(cat);
    this.deleteError.set(null);
    this.isDeleteCategoryModalOpen.set(true);
  }

  closeDeleteCategoryModal() {
    this.isDeleteCategoryModalOpen.set(false);
    this.categoryToDelete.set(null);
    this.deleteError.set(null);
  }

  submitDeleteCategory() {
    const target = this.categoryToDelete();
    if (!target) return;

    this.deleteError.set(null);
    this.softwareService.deleteCategory(target.id).subscribe({
      next: () => {
        this.closeDeleteCategoryModal();
        this.loadCategories();
        this.showToast('success', `Đã xoá nhóm "${target.name}"!`);
      },
      error: (err) => {
        this.deleteError.set(err.problem?.detail || err.error?.detail || 'Không thể xoá nhóm phần mềm này.');
      }
    });
  }

  // --- Vendor Management (Thêm / Sửa / Xoá) ---

  // 1. Create Vendor
  openCreateVendorModal() {
    this.vendorForm.reset({ code: '', name: '', contactInfo: '' });
    this.modalError.set(null);
    this.isCreateVendorModalOpen.set(true);
  }

  closeCreateVendorModal() {
    this.isCreateVendorModalOpen.set(false);
  }

  submitCreateVendor() {
    if (this.vendorForm.invalid) {
      this.vendorForm.markAllAsTouched();
      return;
    }

    const val = this.vendorForm.getRawValue();
    this.modalError.set(null);

    this.softwareService.createVendor({
      code: val.code!,
      name: val.name!,
      contactInfo: val.contactInfo || ''
    }).subscribe({
      next: () => {
        this.closeCreateVendorModal();
        this.loadVendors();
        this.showToast('success', 'Thêm nhà cung cấp thành công!');
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo nhà cung cấp.');
      }
    });
  }

  // 2. Edit Vendor
  openEditVendorModal(v: VendorDto) {
    this.vendorToEdit.set(v);
    this.editVendorForm.patchValue({
      code: v.code,
      name: v.name,
      contactInfo: v.contactInfo || '',
      isActive: v.isActive
    });
    this.modalError.set(null);
    this.isEditVendorModalOpen.set(true);
  }

  closeEditVendorModal() {
    this.isEditVendorModalOpen.set(false);
    this.vendorToEdit.set(null);
  }

  submitEditVendor() {
    if (this.editVendorForm.invalid) {
      this.editVendorForm.markAllAsTouched();
      return;
    }

    const target = this.vendorToEdit();
    if (!target) return;

    const val = this.editVendorForm.getRawValue();
    this.modalError.set(null);

    this.softwareService.updateVendor(target.id, {
      name: val.name!,
      contactInfo: val.contactInfo || '',
      isActive: val.isActive ?? true
    }).subscribe({
      next: () => {
        this.closeEditVendorModal();
        this.loadVendors();
        this.showToast('success', `Đã cập nhật nhà cung cấp "${val.name}" thành công!`);
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể cập nhật nhà cung cấp.');
      }
    });
  }

  // 3. Delete Vendor
  openDeleteVendorModal(v: VendorDto) {
    this.vendorToDelete.set(v);
    this.deleteError.set(null);
    this.isDeleteVendorModalOpen.set(true);
  }

  closeDeleteVendorModal() {
    this.isDeleteVendorModalOpen.set(false);
    this.vendorToDelete.set(null);
    this.deleteError.set(null);
  }

  submitDeleteVendor() {
    const target = this.vendorToDelete();
    if (!target) return;

    this.deleteError.set(null);
    this.softwareService.deleteVendor(target.id).subscribe({
      next: () => {
        this.closeDeleteVendorModal();
        this.loadVendors();
        this.showToast('success', `Đã xoá nhà cung cấp "${target.name}"!`);
      },
      error: (err) => {
        this.deleteError.set(err.problem?.detail || err.error?.detail || 'Không thể xoá nhà cung cấp này.');
      }
    });
  }
}

