import { ChangeDetectorRef, Component } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { NavbarComponent } from "./navbar/navbar.component";
import { filter } from 'rxjs';
import { CommonModule } from '@angular/common';
import { CurrentUser } from './models/current-user';
import { AuthService } from './services/auth-service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'frontend';

  showNavbar = true;

  currentRoute!: string;
  currentUser!: CurrentUser | null;

  constructor(private router: Router, private authService: AuthService) {

      this.router.events.subscribe(() => {
        this.currentRoute = this.router.url;
        this.currentUser = authService.getCurrentUser();

        const hiddenRoutes = ['/login'];
        this.showNavbar = !hiddenRoutes.includes(this.currentRoute);
      });
  }
}
