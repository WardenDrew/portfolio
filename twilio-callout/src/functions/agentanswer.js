/* Sanitized Emergency Callout System
 * Public portfolio excerpt
 *
 * This is invoked if the agent answers the call, asking for DTMF confirmation before joining them to the conference room
*/

const axios = require('axios');
const crypto = require('crypto'); // NodeJS Crypto

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
    return callback(err);
  }

  // Ask the agent if they want to connect
  let twiml = new Twilio.twiml.VoiceResponse();
  twiml.pause({length: 2 });
  twiml.gather({
    action: `https://${context.DOMAIN_NAME}/agentjoin?Conference=${event.Conference}&Label=${event.Label}`,
    method: 'GET',
    timeout: 10,
    numDigits: 1
  }).say({
    voice: config.voice,
    language: config.lang
  }, config.messages.agentAnswer);

  return callback(null, twiml);
};
