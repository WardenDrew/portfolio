/* Sanitized Emergency Callout System
 * Public portfolio excerpt
 *
 * This calls the agent specified in the query parameters
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

  // Get the twilio client from the context to place outbound calls
	let twilioClient = context.getTwilioClient();

  // Place a call to the agent we're intending to call
  try {
    await twilioClient.calls
      .create({
        url: `https://${context.DOMAIN_NAME}/agentanswer?Conference=${event.Conference}&Label=${event.Label}`,
        from: config.from,
        to: event.To
      });
  }
  catch (err) {
    console.log(err);
    console.error(err);
    return callback(err);
  }

  // Return success
  return callback(null, 'success');
};
