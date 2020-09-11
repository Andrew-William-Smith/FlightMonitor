import React from 'react';

import Header from './Header/Header';

export default class App extends React.Component<{}, {}> {
    public render(): React.ReactNode {
        return (
            <Header />
        );
    }
}