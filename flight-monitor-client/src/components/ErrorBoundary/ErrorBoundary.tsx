import React from 'react';

import './ErrorBoundary.scss';

interface IErrorBoundaryProps {
    children: React.ReactNode;
}

interface IErrorBoundaryState {
    error: Error | null;
}

export default class ErrorBoundary extends React.Component<IErrorBoundaryProps, IErrorBoundaryState> {
    state: IErrorBoundaryState = {
        error: null
    };

    static getDerivedStateFromError(err: Error): Partial<IErrorBoundaryState> {
        return { error: err };
    }

    public render(): React.ReactNode {
        if (this.state.error) {
            // An error has occurred, so render the error boundary
            return (
                <div id="error-parent">
                    <div id="error-boundary">
                        <div id="error-title">FLIGHT MONITOR CRASHED</div>
                        <div id="error-info">
                            <p>
                                Flight Monitor encountered a fatal error from which it was unable to recover.
                                Please report the error below to the developer and reload the page to resume monitoring.
                            </p>

                            <p><strong>Error type:</strong>&nbsp;<span id="error-type">{this.state.error.name}</span></p>
                            <p><strong>Message:</strong> {this.state.error.message}</p>

                            <div id="reload-button" onClick={() => window.location.reload()}>
                                RELOAD PAGE
                            </div>
                        </div>
                    </div>
                </div>
            );
        } else {
            // No error has occurred, so we'll render the normal contents
            return this.props.children;
        }
    }
}