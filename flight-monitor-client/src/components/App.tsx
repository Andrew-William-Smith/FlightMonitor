import React from 'react';
import { observer, inject } from 'mobx-react';

import ApplicationStore from '../stores/ApplicationStore';

interface IAppProps {
  globalStore?: ApplicationStore;
}

@inject('globalStore')
@observer
export default class App extends React.Component<IAppProps, {}> {
  public render(): React.ReactNode {
    return (
      <div>Hello!</div>
    );
  }
}