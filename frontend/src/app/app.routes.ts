import { Routes } from '@angular/router';
import { ConvertComponent } from './convert/convert.component';
import { LoginComponent } from './login/login.component';
import { AuthGuard } from './guards/auth-guard';
import { GoogleAuthCallbackComponent } from './google-auth-callback/google-auth-callback.component';

export const routes: Routes = [
    { path: 'convert', component: ConvertComponent, canActivate: [AuthGuard] },
    { path: 'login', component: LoginComponent },
    { path: 'auth/google/callback', component: GoogleAuthCallbackComponent },
    { path: '**', redirectTo: 'convert' }
];
