import React from 'react';
import { inject, observer } from 'mobx-react';

import './VariometerGauge.scss';
import IGauge from '../IGauge';

import dial from './img/dial.svg';
import hand from './img/hand.svg';

@inject('globalStore')
@observer
export default class VariometerGauge extends React.Component<IGauge, {}> {
    public constructor(props: IGauge) {
        super(props);
        this.props.globalStore!.addVariable('VERTICAL SPEED');
    }

    public render(): React.ReactNode {
        // Formula derived directly from Skyhawk-Flight-Instruments
        let speed = (this.props.globalStore!.simState['VERTICAL SPEED']?.value ?? 0) / -60;
        let speedRotation = Math.sign(speed) * Math.min(Math.abs(speed), 20) * 8.2;
        if (speed > 20 || speed < 20) {
            speedRotation += speed % 2;
        }

        let size = this.props.size;
        let sizeStyle = {
            width: `${size}px`,
            height: `${size}px`,
            margin: `-${size * 0.2}px`
        };

        return (
            <div className="gauge-wrapper">
                <div className="variometer-gauge">
                    <img className="variometer-hand" style={{ transform: `rotate(-${speedRotation}deg)`, ...sizeStyle }} src={hand} alt="" />
                    <img className="variometer-dial" style={sizeStyle} src={dial} alt="" />
                </div>
            </div>
        );
    }
}