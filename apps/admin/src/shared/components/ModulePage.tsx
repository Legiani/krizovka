'use client';

import React from 'react';
import { Alert, Button, Card, Spinner } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { faInbox, faSync } from '@fortawesome/free-solid-svg-icons';
import { PageHeader } from './PageHeader';

interface ModulePageProps {
    title: React.ReactNode;
    subtitle?: React.ReactNode;
    actions?: React.ReactNode;
    filters?: React.ReactNode;
    children: React.ReactNode;
    isLoading?: boolean;
    isEmpty?: boolean;
    emptyMessage?: string;
    emptyIcon?: IconDefinition;
    footer?: React.ReactNode;
    noCard?: boolean;
    /** Předej error z react-query nebo useQuery — zobrazí standardizovaný alert s HTTP kódem */
    error?: unknown;
    /** Callback pro retry tlačítko v error stavu (typicky `refetch`) */
    onRetry?: () => void;
}
import { getErrorStatus, getErrorMessage } from '@/shared/types/api-errors';

export const ModulePage: React.FC<ModulePageProps> = ({
    title,
    subtitle,
    actions,
    filters,
    children,
    isLoading = false,
    isEmpty = false,
    emptyMessage = 'Zatím zde nejsou žádná data',
    emptyIcon = faInbox,
    footer,
    noCard = false,
    error,
    onRetry,
}) => {
    // Extrahuj HTTP status a zprávu z axios error objektu
    const errorStatus = getErrorStatus(error);
    const errorMessage = getErrorMessage(error);

    return (
        <div className="container-fluid p-4">
            <PageHeader
                title={title}
                subtitle={subtitle}
                action={actions}
            />

            {filters && (
                <div className="card mb-4 border-0 shadow-sm">
                    <div className="card-body">
                        {filters}
                    </div>
                </div>
            )}

            {/* Standardizovaný error stav — zobrazí se místo obsahu */}
            {error && !isLoading ? (
                <Alert variant="danger" className="d-flex align-items-start gap-3">
                    <div className="flex-grow-1">
                        <Alert.Heading className="h6 mb-1">Nepodařilo se načíst data</Alert.Heading>
                        {errorStatus && <span className="badge bg-danger me-2">HTTP {errorStatus}</span>}
                        <span className="text-danger-emphasis small">{errorMessage}</span>
                    </div>
                    {onRetry && (
                        <Button variant="outline-danger" size="sm" onClick={onRetry} className="flex-shrink-0">
                            <FontAwesomeIcon icon={faSync} className="me-1" /> Zkusit znovu
                        </Button>
                    )}
                </Alert>
            ) : noCard ? (
                <>
                    {isLoading ? (
                        <Card className="border-0 shadow-sm p-5 text-center">
                            <Spinner animation="border" variant="primary" />
                        </Card>
                    ) : isEmpty ? (
                        <Card className="border-0 shadow-sm p-5 text-center text-muted">
                            <FontAwesomeIcon icon={emptyIcon} size="3x" className="mb-3 opacity-25" />
                            <p className="mb-0">{emptyMessage}</p>
                        </Card>
                    ) : (
                        children
                    )}
                    {footer && (
                        <div className="mt-3">
                            {footer}
                        </div>
                    )}
                </>
            ) : (
                <div className="card border-0 shadow-sm overflow-hidden">
                    <div className="card-body p-0">
                        {isLoading ? (
                            <div className="text-center p-5">
                                <Spinner animation="border" variant="primary" role="status">
                                    <span className="visually-hidden">Načítání…</span>
                                </Spinner>
                            </div>
                        ) : isEmpty ? (
                            <div className="text-center p-5 text-muted">
                                <FontAwesomeIcon icon={emptyIcon} size="3x" className="mb-3 opacity-25" />
                                <p className="mb-0">{emptyMessage}</p>
                            </div>
                        ) : (
                            <div className="table-responsive">
                                {children}
                            </div>
                        )}
                    </div>
                    {footer && (
                        <div className="card-footer bg-white border-top-0 p-3">
                            {footer}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};
