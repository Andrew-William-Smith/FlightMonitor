import React from 'react';
import ReactDOM from 'react-dom';
import { Provider } from 'mobx-react';

import './styles/index.scss';
import App from './components/App';
import ApplicationStore from './stores/ApplicationStore';
import ErrorBoundary from './components/ErrorBoundary/ErrorBoundary';

// Create a global instance of the application store
const GLOBAL_STORE = new ApplicationStore();

ReactDOM.render(
  <React.StrictMode>
    <Provider globalStore={GLOBAL_STORE}>
      <ErrorBoundary>
        <App />
      </ErrorBoundary>
    </Provider>
  </React.StrictMode>,
  document.getElementById('root')
);