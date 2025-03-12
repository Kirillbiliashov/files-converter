import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../services/auth-service';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [],
  templateUrl: './oauth-callback.component.html',
  styleUrl: './oauth-callback.component.css'
})
export class OauthCallbackComponent implements OnInit {
  errorMessage: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) { }

  
  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const authCode = params['code'];
      const provider = params['state'];
      console.log(`auth code: ${authCode}`);

      setTimeout(() => {
        this.authService.processOauthLogin(authCode, provider).subscribe({
          next: () => this.router.navigate(['/convert']),
          error: err => {
            this.errorMessage = err.error;
          }
        })
      }, 10000);
    });
  }
}
