import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { CurrentUser } from '../models/current-user';
import { AuthResult } from '../models/auth-result';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private tokenKey = 'jwtToken';
  private tokenExpirationKey = 'jwtTokenExpirationDate';
  private userKey = 'currentUser';

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${environment.apiBaseUrl}/auth/login`, { email, password })
      .pipe(
        tap(response => {
          this.storeAuthData(response);
        })
      );
  }

  register(username: string, email: string, password: string): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${environment.apiBaseUrl}/auth/register`, { username, email, password })
      .pipe(
        tap(response => {
          this.storeAuthData(response);
        })
      );
  }

  processOauthLogin(code: string, provider: string): Observable<AuthResult> {
    const body = { code, provider };
    return this.http.post<AuthResult>(`${environment.apiBaseUrl}/auth/login/oauth/process`, body)
      .pipe(
        tap(response => {
          this.storeAuthData(response);
        })
      );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): CurrentUser | null {
    const tokenExpirationValue = localStorage.getItem(this.tokenExpirationKey);
    if (tokenExpirationValue && new Date(tokenExpirationValue.replace(/"/g, '')) <= new Date()) {
      this.logout(); 
      return null;
    }

    const userValue = localStorage.getItem(this.userKey);
    return userValue ? JSON.parse(userValue) : null;
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.tokenExpirationKey);
    this.router.navigate(['/login']);
  }

  private storeAuthData(response: AuthResult) {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.tokenExpirationKey, JSON.stringify(response.tokenExpirationDate));
    
    if (response.user) {
      localStorage.setItem(this.userKey, JSON.stringify(response.user));
    }
  }

  getGoogleAccessToken() {
    return this.http.get<{ accessToken: string }>(`${environment.apiBaseUrl}/auth/access-token?provider=Google`)
  }

}
