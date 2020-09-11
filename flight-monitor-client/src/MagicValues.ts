/**
 * A collection of magic values that Microsoft/Asobo deemed acceptable to
 * present via the SimConnect API.
 */
export default class Magic {
    public static readonly ATC_MODEL: { [id: string]: string } = {
        'TT:ATCCOM.AC_MODEL_A20N.0.text': 'A320neo',
        'TT:ATCCOM.AC_MODEL A5.0.text':   'A5',
        'TT:ATCCOM.AC_MODEL B350.0.text': 'King Air 350i',
        'TT:ATCCOM.AC_MODEL_B748.0.text': '747-8 Intercontinental',
        'TT:ATCCOM.AC_MODEL_B78X.0.text': '787-10 Dreamliner',
        'TT:ATCCOM.AC_MODEL_BE36.0.text': 'Bonanza G36',
        'TT:ATCCOM.AC_MODEL_BE58.0.text': 'Baron G58',
        'TT:ATCCOM.AC_MODEL C152.0.text': '152',
        'TT:ATCCOM.AC_MODEL C172.0.text': '172 Skyhawk',
        'TT:ATCCOM.AC_MODEL C208.0.text': '208 B Grand Caravan EX',
        'TT:ATCCOM.AC_MODEL_C25C.0.text': 'Citation CJ4',
        'TT:ATCCOM.AC_MODEL_C700.0.text': 'Citation Longitude',
        'TT:ATCCOM.AC_MODEL_CC19.0.text': 'XCub',
        'TT:ATCCOM.AC_MODEL CP10.0.text': 'Cap10',
        'TT:ATCCOM.AC_MODEL_DA40.0.text': 'DA40',
        'TT:ATCCOM.AC_MODEL_DA62.0.text': 'DA62',
        'TT:ATCCOM.AC_MODEL_DR40.0.text': 'DR400/100 Cadet',
        'TT:ATCCOM.AC_MODEL DV20.0.text': 'DV20',
        'TT:ATCCOM.AC_MODEL E300.0.text': '330LT',
        'TT:ATCCOM.AC_MODEL_FDCT.0.text': 'CTSL',
        'TT:ATCCOM.AC_MODEL_PIVI.0.text': 'Virus SW121',
        'TT:ATCCOM.AC_MODEL PTS2.0.text': 'Pitts Special S2S',
        'TT:ATCCOM.AC_MODEL_SAVG.0.text': 'Savage Cub',    // Also Savage Shock Ultra, but no way to differentiate other than the title
        'TT:ATCCOM.AC_MODEL_SR22.0.text': 'SR22',
        'TT:ATCCOM.AC_MODEL_TBM9.0.text': 'TBM 930',
        '$$:VL3':                         'VL3'
    };

    public static readonly ATC_TYPE: { [id: string]: string } = {
        'TT:ATCCOM.ATC_NAME AIRBUS.0.text':       'Airbus',
        'TT:ATCCOM.ATC_NAME_BEECHCRAFT.0.text':   'Beechcraft',
        'TT:ATCCOM.ATC_NAME BOEING.0.text':       'Boeing',
        'TT:ATCCOM.ATC_NAME CESSNA.0.text':       'Cessna',
        'TT:ATCCOM.ATC_NAME_CIRRUS.0.text':       'Cirrus',
        'TT:ATCCOM.ATC_NAME_CUBCRAFTERS.0.text':  'CubCrafters',
        'TT:ATCCOM.ATC_NAME_DAHER.0.text':        'Daher',
        'TT:ATCCOM.ATC_NAME DIAMOND.0.text':      'Diamond Aircraft',
        'TT:ATCCOM.ATC_NAME EXTRA.0.text':        'Extra',
        'TT:ATCCOM.ATC_NAME_FLIGHTDESIGN.0.text': 'Flight Design',
        'TT:ATCCOM.ATC_NAME_ICON.0.text':         'Icon',
        'TT:ATCCOM.ATC_NAME_PIPISTREL.0.text':    'Pipistrel',
        'TT:ATCCOM.ATC_NAME PITTS.0.text':        'Aviat',
        'TT:ATCCOM.ATC_NAME_ROBIN.0.text':        'Robin',
        'TT:ATCCOM.ATC_NAME_SAVAGE.0.text':       'Zlin Aviation',
        '$$:JMB Aviation':                        'JMB Aircraft'
    };
}