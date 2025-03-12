import { Routes } from '@angular/router';
import { ConvertComponent } from './convert/convert.component';
import { LoginComponent } from './login/login.component';
import { AuthGuard } from './guards/auth-guard';
import { OauthCallbackComponent } from './oauth-callback/oauth-callback.component';

export const routes: Routes = [
    { path: 'convert', component: ConvertComponent, canActivate: [AuthGuard] },
    { path: 'login', component: LoginComponent },
    { path: 'oauth/callback', component: OauthCallbackComponent },
    { path: '**', redirectTo: 'convert' }
];
