import React from 'react';

import SimString from '../SimTypes/SimString';

export default class Header extends React.Component<{}, {}> {
    public render(): React.ReactNode {
        return (
            <div>
                <SimString variable='TITLE' /><br/>
                <SimString variable='ATC AIRLINE' /><br/>
                <SimString variable='ATC FLIGHT NUMBER' /><br/>
                <SimString variable='ATC ID' /><br/>
                <SimString variable='ATC MODEL' /><br/>
                <SimString variable='ATC TYPE' />
            </div>
        );
    }
}