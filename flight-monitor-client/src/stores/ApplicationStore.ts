import { action, observable } from 'mobx';

/** A variable declaration received from the server. */
export interface ISimVariable {
    /** The ID by which the variable is identified in transmissions. */
    id: number;
    /** The human-readable(-ish) name of the variable. */
    name: string;
    /** The physical unit in which the variable is measured, or its type. */
    unit: string;
}

export interface IApplicationStore {
    /** The current simulation state as reported by the server. */
    simState: Object;
    /** The variables being monitored, as declared by the server. */
    simVariables: { [id: number]: ISimVariable; };
    /** The WebSocket used for server communication. */
    socket: WebSocket;
}

export default class ApplicationStore implements IApplicationStore {
    @observable public simState: Object;
    @observable public simVariables: { [id: number]: ISimVariable; };
    public socket: WebSocket;

    public constructor() {
        // Initialise the WebSocket
        this.simState = {};
        this.simVariables = {};
        this.socket = new WebSocket('ws://localhost:8000/');
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
        this.addVariable('GENERAL ENG THROTTLE LEVER POSITION:1');
        this.addVariable('INDICATED ALTITUDE');
    }

    /** Handler for messages received via WebSocket. */
    @action.bound
    private handleSocketMessage(evt: MessageEvent): void {
        let message = JSON.parse(evt.data);
        console.log(`Received WebSocket message of type ${message['type']}`);

        switch (message['type']) {
            case 'DECLARE_VARIABLE':
                // Add the declared variable to the variables list
                let { id, name, unit } = message;
                this.simVariables[id] = { id, name, unit };
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