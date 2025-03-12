import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../services/auth-service';

@Component({
  selector: 'app-dropbox-auth-callback',
  standalone: true,
  imports: [],
  templateUrl: './dropbox-auth-callback.component.html',
  styleUrl: './dropbox-auth-callback.component.css'
})
export class DropboxAuthCallbackComponent implements OnInit {

    errorMessage: string | null = null;
  
    constructor(
      private route: ActivatedRoute,
      private authService: AuthService,
      private router: Router
    ) { }
  
  
    ngOnInit(): void {
      this.route.queryParams.subscribe(params => {
        const authCode = params['code'];
        console.log(`auth code: ${authCode}`);
  
        setTimeout(() => {
          this.authService.processDropboxLogin(authCode).subscribe({
            next: () => this.router.navigate(['/convert']),
            error: err => {
              this.errorMessage = err.error;
            }
          })
        }, 10000);
      });
    }

}
