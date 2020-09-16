import { IGlobalStore } from "../stores/ApplicationStore";

export default interface IGauge extends IGlobalStore {
    /** The width and height of the gauge. */
    size: number;
}