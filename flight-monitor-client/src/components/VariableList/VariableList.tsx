import React from 'react';
import ApplicationStore from '../../stores/ApplicationStore';
import { inject, observer } from 'mobx-react';

interface IVariableListProps {
    globalStore?: ApplicationStore;
}

@inject('globalStore')
@observer
export default class VariableList extends React.Component<IVariableListProps, {}> {
    public constructor(props: IVariableListProps) {
        super(props);
    }

    public render(): React.ReactNode {
        let { simVariables } = this.props.globalStore!;
        let renderedVars = Object.values(simVariables).map(variable => {
            let { id, name, unit } = variable;
            return (
                <tr>
                    <td>{id}</td><td>{name}</td><td>{unit}</td>
                </tr>
            );
        });

        return (
            <table>
                <thead>
                    <tr>
                        <th>ID</th><th>Name</th><th>Unit</th>
                    </tr>
                </thead>
                <tbody>
                    {renderedVars}
                </tbody>
            </table>
        );
    }
}