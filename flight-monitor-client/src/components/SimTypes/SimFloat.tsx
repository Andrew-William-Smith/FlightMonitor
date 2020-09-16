import React from 'react';
import { inject, observer } from 'mobx-react';

import { IGlobalStore } from '../../stores/ApplicationStore';

interface ISimFloat extends IGlobalStore {
    /** The variable to be displayed by this float view. */
    variable: string;
}

@inject('globalStore')
@observer
export default class SimString extends React.Component<ISimFloat, {}> {
    public constructor(props: ISimFloat) {
        super(props);
        // Add the variable being displayed to the application store
        this.props.globalStore!.addVariable(this.props.variable);
    }

    public render(): React.ReactNode {
        let { globalStore, variable } = this.props;
        let varValue = globalStore!.simState[variable];
        if (varValue === undefined) {
            // TODO: Write an actual loading component
            return <span>Loading...</span>;
        } else {
            return <span>{varValue.value.toFixed(2)}</span>;
        }
    }
}