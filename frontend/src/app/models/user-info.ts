export class UserInfo {
    constructor(
        public username: string,
        public email: string, 
        public lastLogin: Date,
        public created: Date,
        public providers: LoginProvider[]
    ) {}
}

export class LoginProvider {
    constructor(
        public provider: string
    ) {}
}