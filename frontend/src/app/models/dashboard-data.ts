export class DashboardData {
    constructor(
        public conversions: LastActivity,
        public downloaded: LastActivity,
        public uploaded: LastActivity,
        public successRate: LastActivity,
        public analytics: Record<string, number>,
        public activity: Conversion[]
    ) {

    }
}

class LastActivity {
    constructor(public total: number, public difference: number | null) {}
}

export class Conversion {
    constructor(
        public idInternal: string,
        public filename: string,
        public fileSize: number,
        public inputFormat: string,
        public outputFormat: string,
        public status: string,
        public date: Date
    ) {}
}