export interface LicenseAllocationDto {
  id: string;
  entitlementId: string;
  deploymentId: string;
  softwareName: string;
  organizationName: string;
  environment: string;
  instanceKey: string;
  quantity: number;
  allocatedAt: string;
}

export interface LicenseEntitlementDto {
  id: string;
  contractItemId: string;
  licenseType: 'Seat' | 'Unlimited';
  quantity?: number;
  validFrom: string;
  validTo: string;
  allocatedQuantity: number;
  remainingQuantity?: number;
}

export interface ContractItemDto {
  id: string;
  contractId: string;
  softwareId: string;
  softwareName: string;
  description?: string;
  amount?: number; // null if user lacks contracts.read
  entitlements: LicenseEntitlementDto[];
}

export interface ContractDto {
  id: string;
  contractNo: string;
  owningOrganizationId: string;
  owningOrganizationName: string;
  vendorId: string;
  vendorName: string;
  status: 'Draft' | 'Active' | 'Expired' | 'Terminated';
  signedDate: string;
  startDate: string;
  endDate: string;
  totalAmount?: number; // null if user lacks contracts.read
  currencyCode: string;
  maintenanceStartDate?: string;
  maintenanceEndDate?: string;
  version: number;
  items: ContractItemDto[];
}

export interface ContractFilter {
  search?: string;
  organizationId?: string;
  vendorId?: string;
  status?: string;
  page: number;
  pageSize: number;
}

export interface CreateContractRequest {
  contractNo: string;
  owningOrganizationId: string;
  vendorId: string;
  status: string;
  signedDate: string;
  startDate: string;
  endDate: string;
  totalAmount: number;
  currencyCode: string;
  maintenanceStartDate?: string;
  maintenanceEndDate?: string;
}

export interface UpdateContractRequest {
  contractNo: string;
  owningOrganizationId: string;
  vendorId: string;
  status: string;
  signedDate: string;
  startDate: string;
  endDate: string;
  totalAmount: number;
  currencyCode: string;
  maintenanceStartDate?: string;
  maintenanceEndDate?: string;
}

export interface CreateContractItemRequest {
  softwareId: string;
  description?: string;
  amount: number;
}

export interface CreateLicenseEntitlementRequest {
  licenseType: 'Seat' | 'Unlimited';
  quantity?: number;
  validFrom: string;
  validTo: string;
}

export interface CreateAllocationRequest {
  entitlementId: string;
  deploymentId: string;
  quantity: number;
}

export interface UpdateAllocationRequest {
  quantity: number;
}
