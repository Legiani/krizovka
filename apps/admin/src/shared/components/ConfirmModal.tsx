'use client';

import React from 'react';
import { Modal, Button, Spinner } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTrash, faExclamationTriangle } from '@fortawesome/free-solid-svg-icons';

interface ConfirmModalProps {
    show: boolean;
    onHide: () => void;
    onConfirm: () => void;
    title?: string;
    message: React.ReactNode;
    /** Optional red warning text shown below the main message */
    warning?: string;
    confirmLabel?: string;
    confirmVariant?: string;
    isPending?: boolean;
}

/**
 * Shared confirmation dialog — replaces window.confirm() across all modules.
 * Use for any destructive action (delete, suspend, etc.)
 */
export const ConfirmModal = ({
    show,
    onHide,
    onConfirm,
    title = 'Potvrdit smazání',
    message,
    warning,
    confirmLabel = 'Smazat',
    confirmVariant = 'danger',
    isPending = false,
}: ConfirmModalProps) => (
    <Modal show={show} onHide={onHide} centered>
        <Modal.Header closeButton closeLabel="Zavřít">
            <Modal.Title className={`text-${confirmVariant} fw-bold`}>
                {title}
            </Modal.Title>
        </Modal.Header>
        <Modal.Body>
            <p className="mb-0">{message}</p>
            {warning && (
                <p className="text-danger small mt-3 mb-0">
                    <FontAwesomeIcon icon={faExclamationTriangle} className="me-2" />
                    {warning}
                </p>
            )}
        </Modal.Body>
        <Modal.Footer>
            <Button variant="light" onClick={onHide} disabled={isPending}>
                Zrušit
            </Button>
            <Button variant={confirmVariant} onClick={onConfirm} disabled={isPending}>
                {isPending
                    ? <Spinner animation="border" size="sm" />
                    : <><FontAwesomeIcon icon={faTrash} className="me-2" />{confirmLabel}</>
                }
            </Button>
        </Modal.Footer>
    </Modal>
);
