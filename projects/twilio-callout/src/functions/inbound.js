/* Sanitized Emergency Callout System
 * Public portfolio excerpt
 *
 * This is the entrypoint that is registered for Twilio to invoke when a call comes in
*/

const axios = require('axios');
const crypto = require('crypto'); // NodeJS Crypto

const FALLBACK_MESSAGE = "The emergency callout application experienced an error. If this is a life safety emergency, call 9, 1, 1. Otherwise please try again later. Goodbye.";

exports.handler = async (context, event, callback) => {
  // Get config file, fail out if config can't be loaded
  let config = null;
  try {
    let configUri = `https://${context.DOMAIN_NAME}/config.json`;

    // Calculate the twilio signature header
    let digest = crypto.createHmac('sha1', context.AUTH_TOKEN)
      .update(configUri)
      .digest('base64');

    // Load config from assets
    let configResp = await axios.get(configUri, {
      headers: {
        'X-Twilio-Signature': digest
      }
    });
    if (configResp.status != 200) {
      throw new Error("Failed to get configuration");
    }

    config = configResp.data;
  }
  catch (err) {
    console.log(err);
    console.error(err);
    
    // Return a FALLBACK_MESSAGE defined above and hangup
    let twimlError = new Twilio.twiml.VoiceResponse();
    twimlError.say({
      voice: 'Polly.Matthew',
      language: 'en-US'
    }, FALLBACK_MESSAGE);
    twimlError.hangup();
    return callback(null, twimlError);
  }

  // Play the intro message and request any dtmf to screen auto-dialers
  let twiml = new Twilio.twiml.VoiceResponse();
  twiml.pause({length: 2 });
  twiml.gather({
    action: `https://${context.DOMAIN_NAME}/startcallout`,
    method: 'GET',
    timeout: 10,
    numDigits: 1
  }).say({
    voice: config.voice,
    language: config.lang
  }, config.messages.inbound);

  return callback(null, twiml);
};
