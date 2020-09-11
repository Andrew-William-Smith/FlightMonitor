import React from 'react';
import { inject, observer } from 'mobx-react';

import SimString from '../SimTypes/SimString';
import Magic from '../../MagicValues';
import { IGlobalStore } from '../../stores/ApplicationStore';
import './Header.scss';

@inject('globalStore')
@observer
export default class Header extends React.Component<IGlobalStore, {}> {
    public constructor(props: IGlobalStore) {
        super(props);
        // We need the flight number to determine the header name format
        this.props.globalStore!.addVariable('ATC FLIGHT NUMBER');
    }

    public render(): React.ReactNode {
        let flightNum = this.props.globalStore!.simState['ATC FLIGHT NUMBER'];
        let aircraftMake = <SimString variable='ATC TYPE'
                                      transform={t => Magic.ATC_TYPE[t]} />;
        let flightName;

        if (!(flightNum?.value?.length > 0)) {
            // Display as a GA flight without registration
            flightName = (
                <div id='flight-name'>
                    {aircraftMake}&nbsp;<SimString variable='ATC ID' />
                </div>
            );
        } else {
            // This flight is registered as an airline
            flightName = (
                <div id='flight-name'>
                    <SimString variable='ATC AIRLINE' />&nbsp;
                    <SimString variable='ATC FLIGHT NUMBER' />
                </div>
            );
        }

        return (
            <div id='flight-id'>
                {flightName}
                <div id='flight-aircraft'>
                    {aircraftMake}&nbsp;
                    <SimString variable='ATC MODEL'
                               transform={m => Magic.ATC_MODEL[m]} />&nbsp;
                    <span id='tail-number'>
                        #<SimString variable='ATC ID' />
                    </span>
                </div>
            </div>
        );
    }
}