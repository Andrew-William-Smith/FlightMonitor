import React from 'react';
import { inject, observer } from 'mobx-react';

import './AltimeterGauge.scss';
import IGauge from '../IGauge';

import background from './img/background.svg';
import foreground from './img/foreground.svg';
import hand100 from './img/hand100.svg';
import hand1000 from './img/hand1000.svg';
import hand10000 from './img/hand10000.svg';
import pressureInHg from './img/pressureInHg.svg';
import pressureMbar from './img/pressureMbar.svg';

@inject('globalStore')
@observer
export default class AltimeterGauge extends React.Component<IGauge, {}> {
    public constructor(props: IGauge) {
        super(props);
        // Need to know the altimeter settings for proper display
        ['PLANE ALTITUDE',
         'KOHLSMAN SETTING MB'].forEach(v => {
             this.props.globalStore!.addVariable(v);
         });
    }

    public render(): React.ReactNode {
        let state = this.props.globalStore!.simState;
        // Compute rotation of each altitude indicator
        let altitude = state['PLANE ALTITUDE']?.value ?? 0;
        let alt10000 = altitude / 100_000 * 360;
        let alt1000 = altitude / 10_000 * 360;
        let alt100 = altitude / 1000 * 360;

        // Range of millibar pressure indicator is 925-1125, rotated left
        let pressure = state['KOHLSMAN SETTING MB']?.value ?? 925;
        pressure = (pressure - 925) / 200 * -360;

        let size = this.props.size;
        let sizeStyle = {
            width: `${size}px`,
            height: `${size}px`,
            margin: `-${size * 0.2}px`
        };

        return (
            <div className="gauge-wrapper">
                <div className="altimeter-gauge">
                    <img className="altimeter-100" style={{ transform: `rotate(${alt100}deg)`, ...sizeStyle }} src={hand100} alt="" />
                    <img className="altimeter-1000" style={{ transform: `rotate(${alt1000}deg)`, ...sizeStyle }} src={hand1000} alt="" />
                    <img className="altimeter-10000" style={{ transform: `rotate(${alt10000}deg)`, ...sizeStyle }} src={hand10000} alt="" />
                    <img className="altimeter-fg" style={sizeStyle} src={foreground} alt="" />
                    <img className="altimeter-bg" style={sizeStyle} src={background} alt="" />
                    <img className="altimeter-mb" style={{ transform: `rotate(${pressure}deg)`, ...sizeStyle }} src={pressureMbar} alt="" />
                    <img className="altimeter-hg" style={{ transform: `rotate(${pressure}deg)`, ...sizeStyle }} src={pressureInHg} alt="" />
                </div>
            </div>
        );
    }
}