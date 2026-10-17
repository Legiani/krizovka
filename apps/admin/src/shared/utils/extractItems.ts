export function extractItems<T>(data: T[] | { items: T[] } | undefined | null): T[] {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if ('items' in data && Array.isArray(data.items)) return data.items;
    return [];
}
