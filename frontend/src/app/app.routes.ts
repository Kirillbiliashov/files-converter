import { Routes } from '@angular/router';
import { ConvertComponent } from './convert/convert.component';
import { LoginComponent } from './login/login.component';
import { AuthGuard } from './guards/auth-guard';

export const routes: Routes = [
    { path: 'convert', component: ConvertComponent, canActivate: [AuthGuard] },
    { path: 'login', component: LoginComponent },
    { path: '**', redirectTo: 'convert' }
];
