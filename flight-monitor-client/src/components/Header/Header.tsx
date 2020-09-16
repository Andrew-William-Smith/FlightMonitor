import React from 'react';
import { inject, observer } from 'mobx-react';

import SimFloat from '../SimTypes/SimFloat';
import SimString from '../SimTypes/SimString';
import Magic from '../../MagicValues';
import { IGlobalStore } from '../../stores/ApplicationStore';
import './Header.scss';
import HeadingGauge from '../HeadingGauge/HeadingGauge';

@inject('globalStore')
@observer
export default class Header extends React.Component<IGlobalStore, {}> {
    public constructor(props: IGlobalStore) {
        super(props);
        // Need to add a few variables manually for custom displays
        ['ATC AIRLINE',
         'ATC FLIGHT NUMBER',
         'ATC HEAVY',
         'PLANE ALTITUDE'].forEach(v => {
            this.props.globalStore!.addVariable(v);
        });
    }

    public render(): React.ReactNode {
        // Determine the name of the flight
        let { simState } = this.props.globalStore!;
        let aircraftMake = <SimString variable='ATC TYPE' transform={t => Magic.ATC_TYPE[t]} />;
        let heavy = simState['ATC HEAVY']?.value ? 'Heavy' : '';
        let flightName;

        if (!(simState['ATC FLIGHT NUMBER']?.value?.length > 0)) {
            // Display as a GA flight without registration
            flightName = (
                <div id='flight-name'>
                    {aircraftMake}&nbsp;<SimString variable='ATC ID' />&nbsp;{heavy}
                </div>
            );
        } else {
            // This flight is registered as an airline
            flightName = (
                <div id='flight-name'>
                    <SimString variable='ATC AIRLINE' />&nbsp;
                    <SimString variable='ATC FLIGHT NUMBER' />&nbsp;
                    {heavy}
                </div>
            );
        }

        // Altitude requires custom display
        let altitude = simState['PLANE ALTITUDE']?.value ?? 0;
        let flightLevel = Math.floor(altitude / 100).toString().padStart(3, '0');

        return (
            <div id='header'>
                <div id='flight-id'>
                    {flightName}
                    <div id='flight-aircraft'>
                        {aircraftMake}&nbsp;
                        <SimString variable='ATC MODEL' transform={m => Magic.ATC_MODEL[m]} />&nbsp;
                        <span id='tail-number'>
                            #<SimString variable='ATC ID' />
                        </span>
                    </div>
                </div>

                <div id='header-gauges'>
                    <HeadingGauge size={140} />
                </div>

                <div id='header-avionics'>
                    <div id='header-altitude'>
                        {`${Math.floor(altitude)} ft • FL${flightLevel}`}
                    </div>
                    <div id='header-airspeed'>
                        <SimFloat variable='AIRSPEED INDICATED' /> kts
                    </div>
                </div>
            </div>
        );
    }
}