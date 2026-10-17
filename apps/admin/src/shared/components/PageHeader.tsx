import React from 'react';

interface PageHeaderProps {
    title: React.ReactNode;
    subtitle?: React.ReactNode;
    action?: React.ReactNode;
}

export const PageHeader = ({ title, subtitle, action }: PageHeaderProps) => {
    return (
        <div className="d-flex justify-content-between align-items-center flex-wrap gap-3 mb-4">
            <div>
                <h1 className="text-primary fw-bold mb-1">{title}</h1>
                {subtitle && <span className="text-muted">{subtitle}</span>}
            </div>
            {action && (
                <div>
                    {action}
                </div>
            )}
        </div>
    );
}
