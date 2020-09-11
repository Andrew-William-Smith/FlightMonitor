import React from 'react';
import { inject, observer } from 'mobx-react';

import { IGlobalStore } from '../../stores/ApplicationStore';

interface ISimString extends IGlobalStore {
    /** The variable to be displayed by this string view. */
    variable: string;
    /** A function run to transform the value before display. */
    transform?(value: string): string;
}

@inject('globalStore')
@observer
export default class SimString extends React.Component<ISimString, {}> {
    public constructor(props: ISimString) {
        super(props);
        // Add the variable being displayed to the application store
        this.props.globalStore!.addVariable(this.props.variable);
    }

    public render(): React.ReactNode {
        let { globalStore, transform, variable } = this.props;
        let varValue = globalStore!.simState[variable];
        if (varValue === undefined) {
            // TODO: Write an actual loading component
            return <span>Loading...</span>;
        } else {
            // Apply the specified transformation, if one exists
            let transformed = transform?.(varValue.value) ?? varValue.value;
            return <span>{transformed}</span>;
        }
    }
}