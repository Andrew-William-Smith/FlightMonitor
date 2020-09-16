import React from 'react';
import { inject, observer } from 'mobx-react';

import './HeadingGauge.scss';
import IGauge from '../IGauge';
import beacon1 from './img/beacon1.svg';
import beacon2 from './img/beacon2.svg';
import dial from './img/dial.svg';
import plane from './img/plane.svg';

@inject('globalStore')
@observer
export default class HeadingGauge extends React.Component<IGauge, {}> {
    public constructor(props: IGauge) {
        super(props);
        // Need to know the heading for proper display
        ['PLANE HEADING DEGREES MAGNETIC',
         'NAV OBS:1'].forEach(v => {
             this.props.globalStore!.addVariable(v);
         });
    }

    public render(): React.ReactNode {
        let state = this.props.globalStore!.simState;
        let heading = state['PLANE HEADING DEGREES MAGNETIC']?.value ?? 0;
        let obsHeading = state['NAV OBS:1']?.value ?? 0;
        let obsHeadingRel = obsHeading - heading;

        let size = this.props.size;
        let sizeStyle = {
            width: `${size}px`,
            height: `${size}px`,
            margin: `-${size * 0.2}px`
        };

        return (
            <div className="heading-gauge">
                <img className="heading-plane" style={sizeStyle} src={plane} alt="" />
                <img className="heading-dial" style={{ transform: `rotate(-${heading}deg)`, ...sizeStyle }} src={dial} alt="" />
                <img className="heading-beacon2" style={{ transform: `rotate(-${obsHeading}deg)`, ...sizeStyle }} src={beacon2} alt="" />
                <img className="heading-beacon1" style={{ transform: `rotate(${obsHeadingRel}deg)`, ...sizeStyle }} src={beacon1} alt="" />
            </div>
        );
    }
}