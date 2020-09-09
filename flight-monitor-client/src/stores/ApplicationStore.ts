import { action, observable } from 'mobx';

/** A variable declaration received from the server. */
export interface ISimVariable {
    /** The ID by which the variable is identified in transmissions. */
    id: number;
    /** The human-readable(-ish) name of the variable. */
    name: string;
    /** The physical unit in which the variable is measured, or its type. */
    unit: string;
    /** The value of this variable. */
    value: any;
}

export interface IApplicationStore {
    /** The current simulation state as reported by the server. */
    simState: { [id: number]: ISimVariable; };
    /** The WebSocket used for server communication. */
    socket: WebSocket;
}

export default class ApplicationStore implements IApplicationStore {
    /** The variables that will be monitored by default at startup. */
    private static readonly DEFAULT_VARIABLES: string[] = [
        'ATC AIRLINE',
        'ATC FLIGHT NUMBER',
        'ATC HEAVY',
        'ATC ID',
        'ATC MODEL',
        'ATC TYPE',
        'GENERAL ENG THROTTLE LEVER POSITION:1',
        'INDICATED ALTITUDE'
    ];

    @observable public simState: { [id: number]: ISimVariable; };
    public socket: WebSocket;

    public constructor() {
        // Initialise the WebSocket
        this.simState = {};
        this.socket = new WebSocket(`ws://${window.location.host}/`);
        this.socket.onopen = this.handleSocketOpen;
        this.socket.onmessage = this.handleSocketMessage;
    }

    /**
     * Begin monitoring the specified variable.  Sends a request to the server
     * that will make the variable's data available on the next broadcast.
     * 
     * @param variable The variable to begin monitoring.
     */
    @action.bound
    public addVariable(variable: string): void {
        this.sendSocketMessage({
            type: 'ADD_VARIABLE',
            variable
        });
    }

    /** Handler for the WebSocket "onopen" signal. */
    @action.bound
    private handleSocketOpen(): void {
        console.log('WebSocket connection established!');
        ApplicationStore.DEFAULT_VARIABLES.forEach(v => this.addVariable(v));
    }

    /** Handler for messages received via WebSocket. */
    @action.bound
    private handleSocketMessage(evt: MessageEvent): void {
        let message = JSON.parse(evt.data);

        switch (message.type) {
            case 'DECLARE_VARIABLE':
                // Add the declared variable to the variables list
                let { id, name, unit } = message;
                this.simState[id] = { id, name, unit, value: 0 };
                break;
            case 'STATE_SNAPSHOT':
                // Merge the new values into the current simulator state
                let { state } = message;
                Object.keys(state).forEach(id => {
                    this.simState[+id].value = state[id];
                });
                break;
        }
    }

    /**
     * Send a message via the current WebSocket connection.
     * @param data The data to send to the server in JSON format.
     */
    @action.bound
    private sendSocketMessage(data: Object): void {
        try {
            this.socket.send(JSON.stringify(data));
        } catch (err) {
            console.error(`Error sending message: ${err}`);
        }
    }
}