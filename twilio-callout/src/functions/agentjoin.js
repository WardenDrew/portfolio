/* Sanitized Emergency Callout System
 * Public portfolio excerpt
 *
 * This joins the agent into the conference room. (setting it up if they beat the caller somehow)
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

  // Connect the agent to the conference room (or creates it if they're first here)
  // Note that the agent's are set not to end the conference on exit, so if any of them leave
  // the conference stays open
  let twiml = new Twilio.twiml.VoiceResponse();
  twiml.say({
    voice: config.voice,
    language: config.lang
  }, config.messages.agentJoin);

  twiml.dial().conference({
    beep: 'true',
    waitUrl: `https://${context.DOMAIN_NAME}/waiturl`,
    waitMethod: 'GET',
    startConferenceOnEnter: true,
    endConferenceOnExit: false,
    label: event.Label
  }, event.Conference);
  
  return callback(null, twiml);
};
