import React from 'react';
import './App.css';

interface AppState {
  simState: Object
}

export default class App extends React.Component<{}, AppState> {
  // Start off with an empty state, to be populated from socket
  state: AppState = {
    simState: {},
  };

  public render(): React.ReactNode {
    return (
      <div>Hello!</div>
    );
  }
}