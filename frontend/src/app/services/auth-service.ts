import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { CurrentUser } from '../models/current-user';
import { AuthResult } from '../models/auth-result';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:7099/api/auth';
  private tokenKey = 'jwtToken';
  private tokenExpirationKey = 'jwtTokenExpirationDate';
  private userKey = 'currentUser';

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.apiUrl}/login`, { email, password })
      .pipe(
        tap(response => {
          this.storeAuthData(response);
        })
      );
  }

  register(username: string, email: string, password: string): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.apiUrl}/register`, { username, email, password })
      .pipe(
        tap(response => {
          this.storeAuthData(response);
        })
      );
  }

  processOauthLogin(code: string, provider: string): Observable<AuthResult> {
    const body = { code, provider };
    return this.http.post<AuthResult>(`${this.apiUrl}/login/oauth/process`, body)
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
      this.logout(); // Clear expired session
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

}
