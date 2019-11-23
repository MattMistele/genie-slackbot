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
            AnswerOptions answer = parseInput(turnContext.Activity.Text);
            Activity reply;

            if (answer != AnswerOptions.Unknown)
            {
                var nextQuestion = await client.Answer(answer);
                if (client.GuessIsDue(nextQuestion))
                {
                    // If the genie thinks it knows the answer, tell it
                    var guess = await client.GetGuess();
                    reply = MessageFactory.Text($"My guess is: {guess[0].Name} - {guess[0].Description}");
                    reply.Attachments = new List<Attachment> { CreateGuessCard(guess[0]) };
                }
                else
                {
                    // Otherwise, ask the next question
                    reply = MessageFactory.Text($"Question {nextQuestion.Step + 1}: {nextQuestion.Text}");
                    reply.SuggestedActions = getPossibleActions();
                }
            }
            else
            {
                reply = MessageFactory.Text(($"I didn't understand that. Please enter Yes (0), No (1), Don't Know (2), Probably (3), or Probably Not (4)"));
                reply.SuggestedActions = getPossibleActions();
            }

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync($"The genie's out of the bottle. Think of any person (real or fictional), and I'll guess it.");

                    // Start a new game and ask the first question
                    client = new AkinatorClient(Language.English, ServerType.Person);
                    var question = await client.StartNewGame();

                    var reply = MessageFactory.Text($"Question {question.Step + 1}: {question.Text}");
                    reply.SuggestedActions = getPossibleActions();
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            }
        }

        private static Attachment CreateGuessCard(AkinatorGuess guess)
        {
            var heroCard = new HeroCard
            {
                Title = guess.Name,
                Subtitle = guess.Description,
                Images = new List<CardImage> { new CardImage(guess.PhotoPath.ToString()) }
            };

            return heroCard.ToAttachment();
        }

        private AnswerOptions parseInput(string input)
        {
            if (input == "Yes" || input == "0")
                return AnswerOptions.Yes;
            else if (input == "No" || input == "1")
                return AnswerOptions.No;
            else if (input == "Don't know" || input == "2")
                return AnswerOptions.DontKnow;
            else if (input == "Probably" || input == "3")
                return AnswerOptions.Probably;
            else if (input == "Probably Not" || input == "4")
                return AnswerOptions.ProbablyNot;
            else
                return AnswerOptions.Unknown;
        }

        private SuggestedActions getPossibleActions()
        {
            return new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "Yes", Type = ActionTypes.ImBack, Value = "Yes" },
                    new CardAction() { Title = "No", Type = ActionTypes.ImBack, Value = "No" },
                    new CardAction() { Title = "Don't Know", Type = ActionTypes.ImBack, Value = "Don't know" },
                    new CardAction() { Title = "Probably", Type = ActionTypes.ImBack, Value = "Probably" },
                    new CardAction() { Title = "Probably Not", Type = ActionTypes.ImBack, Value = "Probably Not" },
                },
            };
        }
    }
}
