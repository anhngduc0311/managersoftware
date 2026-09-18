import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { SoftwareService, SoftwareDto, SoftwareCategoryDto, VendorDto } from '@core/services/software.service';
import { AuthService } from '@core/services/auth.service';
import { SoftwareDetailComponent } from './software-detail.component';

@Component({
  selector: 'app-software-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SoftwareDetailComponent, FormsModule],
  templateUrl: './software-list.component.html',
  styleUrls: ['./software-list.component.scss']
})
export class SoftwareListComponent implements OnInit {
  private softwareService = inject(SoftwareService);
  public authService = inject(AuthService);
  private fb = inject(FormBuilder);

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
  isCreateCategoryModalOpen = signal<boolean>(false);
  isCreateVendorModalOpen = signal<boolean>(false);
  selectedSoftware = signal<SoftwareDto | null>(null);
  modalError = signal<string | null>(null);

  // Forms
  softwareForm = this.fb.group({
    code: ['', [Validators.required]],
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

  vendorForm = this.fb.group({
    code: ['', [Validators.required]],
    name: ['', [Validators.required]],
    contactInfo: ['']
  });

  ngOnInit(): void {
    this.loadCatalogData();
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

  // Create Software
  openCreateSoftwareModal() {
    this.softwareForm.reset({
      code: '',
      name: '',
      categoryId: '',
      vendorId: '',
      description: '',
      lifecycleStatus: 'Active'
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
      lifecycleStatus: val.lifecycleStatus!
    }).subscribe({
      next: () => {
        this.closeCreateSoftwareModal();
        this.loadSoftware();
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo phần mềm.');
      }
    });
  }

  // Create Category
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
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo nhóm phần mềm.');
      }
    });
  }

  // Create Vendor
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
      },
      error: (err) => {
        this.modalError.set(err.problem?.detail || err.error?.detail || 'Không thể tạo nhà cung cấp.');
      }
    });
  }
}
