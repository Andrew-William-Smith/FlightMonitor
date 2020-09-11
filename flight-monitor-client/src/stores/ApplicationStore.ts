import { action, observable } from 'mobx';

/** Mixin interface for including an injected global store. */
export interface IGlobalStore {
    globalStore?: ApplicationStore;
}

/** A variable declaration received from the server. */
interface ISimVariable {
    /** The ID by which the variable is identified in transmissions. */
    id: number;
    /** The human-readable(-ish) name of the variable. */
    name: string;
    /** The physical unit in which the variable is measured, or its type. */
    unit: string;
    /** The value of this variable. */
    value: any;
}

interface IApplicationStore {
    /** The current simulation state as reported by the server. */
    simState: { [id: string]: ISimVariable; };
}

export default class ApplicationStore implements IApplicationStore {
    @observable public simState: { [id: string]: ISimVariable } = {};
    /** Internal mapping from ID to variable, for socket performance. */
    private idMappings: { [id: number]: ISimVariable; } = {};
    /** The WebSocket over which all server communication will be performed. */
    private socket: WebSocket;
    /** Messages that are waiting to be sent once the WebSocket opens. */
    private messageQueue: object[] = [];

    public constructor() {
        // Initialise the WebSocket
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
        // Don't attempt to re-add a variable that we know is available locally
        if (!(variable in this.simState)) {
            this.sendSocketMessage({
                type: 'ADD_VARIABLE',
                variable
            });
        }
    }

    /** Handler for the WebSocket "onopen" signal. */
    @action.bound
    private handleSocketOpen(): void {
        console.log('WebSocket connection established!');
        // Send all messages waiting in the queue
        this.messageQueue.forEach(msg => this.sendSocketMessage(msg));
        this.messageQueue = [];
    }

    /** Handler for messages received via WebSocket. */
    @action.bound
    private handleSocketMessage(evt: MessageEvent): void {
        let message = JSON.parse(evt.data);

        switch (message.type) {
            case 'DECLARE_VARIABLE':
                // Add the declared variable to the variables list
                let { id, name, unit } = message;
                let newVar = { id, name, unit, value: 0 };
                this.simState[name] = newVar;
                this.idMappings[id] = newVar;
                break;
            case 'STATE_SNAPSHOT':
                // Merge the new values into the current simulator state
                let { state } = message;
                Object.keys(state).forEach(id => {
                    let mapping = this.idMappings[+id];
                    mapping.value = state[id];
                    // Force a re-render on the aliased observable
                    this.simState[mapping.name] = mapping;
                });
                break;
        }
    }

    /**
     * Send a message via the current WebSocket connection.
     * @param data The data to send to the server in JSON format.
     */
    @action.bound
    private sendSocketMessage(data: object): void {
        try {
            if (this.socket.readyState === WebSocket.OPEN) {
                // If the socket is open, go ahead and send the message
                this.socket.send(JSON.stringify(data));
            } else {
                // We must enqueue the message to be sent once the socket opens
                this.messageQueue.push(data);
            }
        } catch (err) {
            console.error(`Error sending message: ${err}`);
        }
    }
}