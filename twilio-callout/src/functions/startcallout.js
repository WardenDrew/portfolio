/* Sanitized Emergency Callout System
 * Public portfolio excerpt
 *
 * This sets up the conference, invokes the endpoints to call out to each agent, and places the caller in the conference while waiting for an agent.
*/

const axios = require('axios');
const crypto = require('crypto'); // NodeJS Crypto

const FALLBACK_MESSAGE = "The emergency callout application experienced an error. If this is a life safety emergency, call 9, 1, 1. Otherwise please try again later. Goodbye.";

exports.handler = async (context, event, callback) => {

  // Generate UUID
  let conferenceUUID = crypto.randomUUID();

  // Log event
  console.log(`Inbound Emergency Call from ${event.From} Conference UUID: ${conferenceUUID}`);

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

  // Build the TWIML to say the inbound message, and setup a conference for agents to join the caller on
	let twiml = new Twilio.twiml.VoiceResponse();
  twiml.say({
    voice: config.voice,
    language: config.lang
  }, config.messages.startCallout);
  
  twiml.dial().conference({
    beep: 'true',
    waitUrl: `https://${context.DOMAIN_NAME}/waiturl`,
    waitMethod: 'GET',
    startConferenceOnEnter: false,
    endConferenceOnExit: true,
    participantLabel: 'caller',
    record: 'record-from-start'
  }, conferenceUUID);


  // Invoke callagent endpoint for each agent
  let calledCount = 0;
  for (let agent of config.agents) {
    // Safety check in case this agent was the one that called
    if (event.From === agent.number) continue;

    // Simple encode for twilio e.164 format phone numbers
    let urlNumber = agent.number.replace('+', '%2b');

    // Remove non alphanumeric characters from name
    let urlName = agent.name.replace(/[^0-9a-z]/gi, '');

    try {
      console.log(`Calling ${agent.name} on ${agent.number}`);

      let callAgentUri = `https://${context.DOMAIN_NAME}/callagent?To=${urlNumber}&Conference=${conferenceUUID}&Label=${urlName}`;
    
      // Calculate the twilio signature header
      let callAgentDigest = crypto.createHmac('sha1', context.AUTH_TOKEN)
        .update(callAgentUri)
        .digest('base64');

      // Invoke callagent
      let callAgentResp = await axios.get(callAgentUri, {
        headers: {
          'X-Twilio-Signature': callAgentDigest
        }
      });
      if (callAgentResp.status != 200) {
        throw new Error("Failed to call agent!");
      }

      calledCount++;
    } catch (err) {
      console.log(err);
      console.error(err);
    }
  }

  // Check if we actually dialed anyone
  if (calledCount == 0) {
    console.log("No agents to dial!");

    let twimlError = new Twilio.twiml.VoiceResponse();
    twimlError.say({
      voice: config.voice,
      language: config.lang
    }, config.messages.noAgents);
    twimlError.hangup();
    return callback(null, twimlError);
  }

  // Run the twiml, playing the message to the caller and setting up the conference
  return callback(null, twiml);
};
