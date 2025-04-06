import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, Input } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../services/auth-service';
import { CurrentUser } from '../models/current-user';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {

  @Input() currentRoute!: string;
  @Input() currentUser!: CurrentUser | null;
  

  constructor(private authService: AuthService, private router: Router) {
  }

  navigateToSettings() {
    this.router.navigate(
      ['/settings'],
      {
        queryParams: { from: this.currentRoute.substring(1) }
      }
      );
  }

  logout() {
    this.authService.logout();
    this.currentUser = null;
    window.location.href = '/login';
  }
}
