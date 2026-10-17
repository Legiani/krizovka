'use client';

import { Table as BTable, Spinner } from 'react-bootstrap';

export interface Column<T> {
    header: string;
    accessor: keyof T | ((row: T, rowIndex: number) => React.ReactNode);
    className?: string;
}

interface TableCustomProps<T> {
    data: T[];
    columns: Column<T>[];
    keyField: keyof T | ((row: T) => string);
    onRowClick?: (row: T) => void;
    isLoading?: boolean;
    caption?: string;
}

export function TableCustom<T>({
    data,
    columns,
    keyField,
    onRowClick,
    caption,
    isLoading = false
}: TableCustomProps<T>) {
    // Helper to get key from row
    const getRowKey = (row: T): string => {
        if (typeof keyField === 'function') {
            return keyField(row);
        }
        return String(row[keyField]);
    };

    return (
        <div className="table-responsive shadow-sm rounded bg-white" tabIndex={0} role="region" aria-label={caption || 'Tabulka'}>
            <BTable hover className="mb-0">
                {caption && <caption className="visually-hidden">{caption}</caption>}
                <thead className="bg-light">
                    <tr>
                        {columns.map((col, idx) => (
                            <th scope="col" key={idx} className={`border-0 py-3 ${col.className || ''}`}>
                                {col.header}
                            </th>
                        ))}
                    </tr>
                </thead>
                <tbody>
                    {isLoading ? (
                        <tr>
                            <td colSpan={columns.length} className="text-center py-5 text-muted">
                                <Spinner animation="border" size="sm" className="me-2" />
                                Načítání...
                            </td>
                        </tr>
                    ) : data.length === 0 ? (
                        <tr>
                            <td colSpan={columns.length} className="text-center py-5 text-muted">
                                Žádná data k zobrazení
                            </td>
                        </tr>
                    ) : (
                        data.map((row, rowIndex) => (
                            <tr
                                key={getRowKey(row)}
                                onClick={() => onRowClick && onRowClick(row)}
                                style={{ cursor: onRowClick ? 'pointer' : 'default' }}
                                className="align-middle"
                            >
                                {columns.map((col, idx) => (
                                    <td key={idx} className="py-3 border-bottom-0">
                                        {typeof col.accessor === 'function'
                                            ? col.accessor(row, rowIndex)
                                            : (row[col.accessor] as React.ReactNode)}
                                    </td>
                                ))}
                            </tr>
                        ))
                    )}
                </tbody>
            </BTable>
        </div>
    );
}
