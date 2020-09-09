import React from 'react';
import { inject, observer } from 'mobx-react';

import ApplicationStore from '../stores/ApplicationStore';
import VariableList from './VariableList/VariableList';

interface IAppProps {
  globalStore?: ApplicationStore;
}

@inject('globalStore')
@observer
export default class App extends React.Component<IAppProps, {}> {
  public render(): React.ReactNode {
    return (
      <VariableList />
    );
  }
}