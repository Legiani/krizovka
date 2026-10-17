import { AxiosError } from 'axios';

export function isApiError(error: unknown): error is AxiosError<{ message?: string; status?: number }> {
    return error instanceof AxiosError || (typeof error === 'object' && error !== null && 'status' in error);
}

export function getErrorStatus(error: unknown): number | undefined {
    if (error instanceof AxiosError) {
        return error.response?.status || error.status;
    }
    if (typeof error === 'object' && error !== null && 'status' in error) {
        return (error as { status: number }).status;
    }
    return undefined;
}

export function getErrorMessage(error: unknown, defaultMessage = 'Neznámá chyba'): string {
    if (error instanceof AxiosError) {
        return error.response?.data?.message || error.message || defaultMessage;
    }
    if (error instanceof Error) {
        return error.message;
    }
    if (typeof error === 'object' && error !== null && 'message' in error) {
        return (error as { message: string }).message;
    }
    return defaultMessage;
}
