import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrganizationService, OrganizationTreeNode } from '@core/services/organization.service';

@Component({
  selector: 'app-organization-tree',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './organization-tree.component.html',
  styleUrls: ['./organization-tree.component.scss']
})
export class OrganizationTreeComponent implements OnInit {
  private orgService = inject(OrganizationService);

  treeNodes = signal<OrganizationTreeNode[]>([]);
  isLoading = signal<boolean>(false);
  expandedNodeIds = signal<Set<string>>(new Set());

  ngOnInit(): void {
    this.loadTree();
  }

  loadTree() {
    this.isLoading.set(true);
    this.orgService.getOrganizationTree().subscribe({
      next: (nodes) => {
        this.treeNodes.set(nodes);
        // Expand all top nodes by default
        const allIds = new Set<string>();
        const traverse = (list: OrganizationTreeNode[]) => {
          for (const node of list) {
            allIds.add(node.id);
            if (node.children?.length) {
              traverse(node.children);
            }
          }
        };
        traverse(nodes);
        this.expandedNodeIds.set(allIds);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  toggleNode(id: string) {
    const current = new Set(this.expandedNodeIds());
    if (current.has(id)) {
      current.delete(id);
    } else {
      current.add(id);
    }
    this.expandedNodeIds.set(current);
  }

  isExpanded(id: string): boolean {
    return this.expandedNodeIds().has(id);
  }
}
