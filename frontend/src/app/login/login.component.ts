import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { passwordMatchValidator } from '../utils/validators';
import { AuthService } from '../services/auth-service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  showLogin = true;

  userForm: FormGroup;

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {
    this.userForm = fb.group({
      email: ['', [Validators.required, Validators.email]],
      confirmPassword: [''],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          // Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&._])[A-Za-z\\d@$!%*?&._]+$')
        ]
      ],
      name: [''],
    });
  }


  toggleFormMode() {
    this.showLogin = !this.showLogin;
    this.setModeValidators();
  }



  setModeValidators() {
    if (this.showLogin) {
      this.userForm.get('name')?.clearValidators();
      this.userForm.get('confirmPassword')?.clearValidators();
      this.userForm.clearValidators();
    } else {
      this.userForm.get('name')?.setValidators([Validators.required]);
      this.userForm.get('confirmPassword')?.setValidators([Validators.required]);
      this.userForm.setValidators(passwordMatchValidator);
    }

    this.userForm.get('name')?.updateValueAndValidity();
    this.userForm.get('confirmPassword')?.updateValueAndValidity();
    this.userForm.updateValueAndValidity();
  }


  login() {
    if (this.userForm.valid) {

      if (this.showLogin) {
        const { email, password } = this.userForm.value;
        this.authService.login(email, password).subscribe({
          next: () => this.router.navigate(['/convert']),
          error: err => {
            console.error(err);
          }
        });
      } else {
        const { name, email, password } = this.userForm.value;

        this.authService.register(name, email, password).subscribe({
          next: () => this.router.navigate(['/convert']),
          error: err => {
            console.error(err);
          }
        });


      }

    } else {
      console.log('Form is invalid');
    }
  }


}
