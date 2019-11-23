using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Akinator.Api.Net.Enumerations;
using Akinator.Api.Net.Model;
using Akinator.Api.Net;

namespace genie_slackbot.Bots
{
    public class EchoBot : ActivityHandler
    {
        static IAkinatorClient client;

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            AnswerOptions answer = (AnswerOptions)Enum.Parse(typeof(AnswerOptions), turnContext.Activity.Text);
            var question = await client.Answer(answer);
            if (client.GuessIsDue(question))
            {
                var guess = await client.GetGuess();
                await turnContext.SendActivityAsync(CreateActivityWithTextAndSpeak($"My guess is: {guess[0].Name} - {guess[0].Description}"), cancellationToken);
            }
            else
            {
                AskQuestion(question, turnContext, cancellationToken);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(CreateActivityWithTextAndSpeak($"The genie's out of the bottle. Think of any person (real or fictional), and I'll guess it."), cancellationToken);

                    // Start a new game and ask the first question
                    client = new AkinatorClient(Language.English, ServerType.Person);
                    var question = await client.StartNewGame();
                    await turnContext.SendActivityAsync(CreateActivityWithTextAndSpeak($"Question {question.Step + 1}: {question.Text}"), cancellationToken);
                }
            }
        }

        private async void AskQuestion(AkinatorQuestion question, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var options = GetOptions();
            string possible = "Possible answers: ";
            foreach (var option in options)
            {
                possible += option.Key;
                possible += ": ";
                possible += option.Value;
                possible += " ";
            }

            await turnContext.SendActivityAsync(CreateActivityWithTextAndSpeak($"Question {question.Step + 1}: {question.Text}"), cancellationToken);
            await turnContext.SendActivityAsync(CreateActivityWithTextAndSpeak(possible), cancellationToken);
        }

        public Dictionary<int, string> GetOptions()
        {
            var myDic = new Dictionary<int, string>();
            foreach (AnswerOptions foo in Enum.GetValues(typeof(AnswerOptions)))
            {
                myDic.Add((int)foo, foo.ToString());
            }

            return myDic;
        }

        private IActivity CreateActivityWithTextAndSpeak(string message)
        {
            var activity = MessageFactory.Text(message);
            string speak = @"<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
              <voice name='Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)'>" +
              $"{message}" + "</voice></speak>";
            activity.Speak = speak;
            return activity;
        }
    }
}
