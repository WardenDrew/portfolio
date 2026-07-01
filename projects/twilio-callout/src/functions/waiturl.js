/* Sanitized Emergency Callout System
 * Public portfolio excerpt
 *
 * Waiting handler when joining the conference room. Plays a beep into the room when the user joins
*/

exports.handler = function(context, event, callback) {
  let twiml = new Twilio.twiml.VoiceResponse();
  twiml.play({
    loop: 0
  }, `https://${context.DOMAIN_NAME}/2secHoldBeep.mp3`);
  return callback(null, twiml);
};
