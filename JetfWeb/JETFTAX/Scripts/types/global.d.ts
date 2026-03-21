declare const Router: {
    action(controller: string, action: string): string;
};

declare const mainApp: ng.IModule;

declare const swal: (options: {
    title?: string;
    text?: string;
    icon?: string;
    timer?: number;
    [key: string]: any;
}) => void;

declare const moment: any;
declare const $: any;

interface ApiResponse<T = any> {
    Redirect?: boolean;
    status?: string;
    msg?: string;
    error?: string;
    ReturnObject?: T;
}
