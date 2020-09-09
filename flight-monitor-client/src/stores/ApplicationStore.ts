import { action, observable } from 'mobx';

export interface IApplicationStore {
    /** The current simulation state as reported by the server. */
    simState: Object;
    /** The WebSocket used for server communication. */
    socket: WebSocket;
}

export default class ApplicationStore implements IApplicationStore {
    @observable public simState: Object;
    public socket: WebSocket;

    public constructor() {
        // Initialise the WebSocket
        this.simState = {};
        this.socket = new WebSocket('ws://localhost:8000/');
        this.socket.onopen = this.handleSocketOpen;
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
            action: 'ADD_VARIABLE',
            variable
        });
    }

    /** Handler for the WebSocket "onopen" signal. */
    @action.bound
    private handleSocketOpen(): void {
        console.log('WebSocket connection established!');
        this.addVariable('INDICATED ALTITUDE');
        this.addVariable('GENERAL ENG THROTTLE LEVER POSITION:1');
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