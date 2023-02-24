using System;
using System.Globalization;
using Discord.Interactions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using pepega_bot.Database.RingFit;
using pepega_bot.Module;

namespace pepega_bot.InteractionModule
{
    internal class RingFitInteractionModule: InteractionModuleBase
    {
        private readonly RingFitModule _ringFitModule;
        private readonly DatabaseService _dbService;

        public RingFitInteractionModule(RingFitModule ringFitModule, DatabaseService dbService)
        {
            _ringFitModule = ringFitModule;
            _dbService = dbService;
        }

        private DateTime ParseDate(string dateStr)
        {
            return DateTime.ParseExact(dateStr, RingFitConstants.DateTimeFormat, CultureInfo.InvariantCulture);
        }

        [ComponentInteraction($"{RingFitConstants.DailyMsgIdentifier}:*,*")]
        public async Task ButtonClickHandler(string identifier, string dateStr)
        {
            var userId = Context.Interaction.User.Id;
            var interaction = Context.Interaction as SocketMessageComponent;
            var messageId = interaction.Message.Id;
            switch (identifier)
            {
                case RingFitConstants.ButtonCustomClickIdentifier:
                    var mb = new ModalBuilder()
                        .WithTitle("Vlastní hodnota")
                        .WithCustomId($"{RingFitConstants.DailyMsgIdentifier}:{dateStr},{userId},{messageId}")
                        .AddTextInput("Minuty", RingFitConstants.CustomValueIdentifier, placeholder: "1234",
                            minLength: 1, maxLength: 3, required: true);
                    await Context.Interaction.RespondWithModalAsync(mb.Build());
                    break;
                case RingFitConstants.ButtonWithValueClickIdentifier:
                    var doubleArg = dateStr.Split(",");
                    var dateSplit = ParseDate(doubleArg[0]);
                    var minuteValue = uint.Parse(doubleArg[1]);

                    await _dbService.InsertOrUpdateRingFitReact(new RingFitReact
                    {
                        MinuteValue = minuteValue,
                        UserId = userId,
                        MessageId = messageId,
                        IsApproximateValue = true,
                        MessageTime = dateSplit
                    });

                    await _ringFitModule.UpdateDailyMessage(dateSplit);
                    await Context.Interaction.RespondAsync($"Vaše hodnota \"{minuteValue}+\" byla zaznamenána.", ephemeral: true);
                    break;
                case RingFitConstants.ButtonRemove:
                    var date = ParseDate(dateStr);
                    var deleted = await _dbService.RemoveRingFitReact(userId, messageId);
                    if (deleted)
                    {
                        await _ringFitModule.UpdateDailyMessage(date);
                        await Context.Interaction.RespondAsync("Váš výsledek byl úspěšně smazán.", ephemeral: true);
                    }
                    else
                    {
                        await Context.Interaction.RespondAsync("Žádný předchozí výsledek za tento den nebyl nalezen.", ephemeral: true);
                    }
                    break;
                default:
                    await Context.Interaction.RespondAsync(
                        "Něco se pokazilo, pokud se tohle stává častěji, kontaktujte autora bota. (RingFitInteractionModule::ButtonClickHandler)", ephemeral: true);
                    break;
            }
        }
    }
}