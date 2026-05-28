using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace BinanceHandler;

public class BinanceCommandHandler : CommandHandlerBase,
    ICommandHandler<CallEndpointCommand>,
    ICommandHandler<GetServerTimeCommand>,
    ICommandHandler<GetExchangeInfoCommand>,
    ICommandHandler<GetTickerPriceCommand>,
    ICommandHandler<GetAveragePriceCommand>,
    ICommandHandler<GetConvertQuoteCommand>,
    ICommandHandler<AcceptConvertQuoteCommand>,
    ICommandHandler<GetConvertOrderStatusCommand>,
    ICommandHandler<GetConvertTradeFlowCommand>
{
    public override IReadOnlyCollection<string> Commands => new[]
    {
        "binance-call",
        "binance-server-time",
        "binance-exchange-info",
        "binance-ticker-price",
        "binance-avg-price",
        "binance-convert-get-quote",
        "binance-convert-accept-quote",
        "binance-convert-order-status",
        "binance-convert-trade-flow"
    };

    public override string ServiceName => "binance";

    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    CallEndpointCommand ICommandHandler<CallEndpointCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<CallEndpointRequest>(payload.GetRawText()) ?? new CallEndpointRequest();
        return new CallEndpointCommand(request);
    }

    GetServerTimeCommand ICommandHandler<GetServerTimeCommand>.Create(JsonElement payload, ILogger logger)
    {
        return new GetServerTimeCommand();
    }

    GetExchangeInfoCommand ICommandHandler<GetExchangeInfoCommand>.Create(JsonElement payload, ILogger logger)
    {
        return new GetExchangeInfoCommand();
    }

    GetTickerPriceCommand ICommandHandler<GetTickerPriceCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetTickerPriceRequest>(payload.GetRawText()) ?? new GetTickerPriceRequest(payload.GetRawText());
        return new GetTickerPriceCommand(request);
    }

    GetAveragePriceCommand ICommandHandler<GetAveragePriceCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetAveragePriceRequest>(payload.GetRawText()) ?? new GetAveragePriceRequest();
        return new GetAveragePriceCommand(request);
    }

    GetConvertQuoteCommand ICommandHandler<GetConvertQuoteCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetConvertQuoteRequest>(payload.GetRawText()) ?? new GetConvertQuoteRequest();
        return new GetConvertQuoteCommand(request);
    }

    AcceptConvertQuoteCommand ICommandHandler<AcceptConvertQuoteCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<AcceptConvertQuoteRequest>(payload.GetRawText()) ?? new AcceptConvertQuoteRequest();
        return new AcceptConvertQuoteCommand(request);
    }

    GetConvertOrderStatusCommand ICommandHandler<GetConvertOrderStatusCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetConvertOrderStatusRequest>(payload.GetRawText()) ?? new GetConvertOrderStatusRequest();
        return new GetConvertOrderStatusCommand(request);
    }

    GetConvertTradeFlowCommand ICommandHandler<GetConvertTradeFlowCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetConvertTradeFlowRequest>(payload.GetRawText()) ?? new GetConvertTradeFlowRequest();
        return new GetConvertTradeFlowCommand(request);
    }

    public override ICommand Create(JsonElement payload, ILogger logger)
    {
        if (payload.ValueKind == JsonValueKind.Object)
        {
            if (payload.TryGetProperty("method", out _))
            {
                return ((ICommandHandler<CallEndpointCommand>)this).Create(payload, logger);
            }
            if (payload.TryGetProperty("quoteId", out _) && !payload.TryGetProperty("orderId", out _))
            {
                return ((ICommandHandler<AcceptConvertQuoteCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("orderId", out _))
            {
                return ((ICommandHandler<GetConvertOrderStatusCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("fromAsset", out _) && payload.TryGetProperty("toAsset", out _))
            {
                return ((ICommandHandler<GetConvertQuoteCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("startTime", out _) || payload.TryGetProperty("endTime", out _))
            {
                return ((ICommandHandler<GetConvertTradeFlowCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("avg", out _))
            {
                return ((ICommandHandler<GetAveragePriceCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("symbol", out _))
            {
                return ((ICommandHandler<GetTickerPriceCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("exchangeInfo", out _))
            {
                return ((ICommandHandler<GetExchangeInfoCommand>)this).Create(payload, logger);
            }
        }

        return ((ICommandHandler<GetServerTimeCommand>)this).Create(payload, logger);
    }

    public override ICommand Create(string command, JsonElement payload, ILogger logger)
    {
        return command switch
        {
            "binance-call" => ((ICommandHandler<CallEndpointCommand>)this).Create(payload, logger),
            "binance-server-time" => ((ICommandHandler<GetServerTimeCommand>)this).Create(payload, logger),
            "binance-exchange-info" => ((ICommandHandler<GetExchangeInfoCommand>)this).Create(payload, logger),
            "binance-ticker-price" => ((ICommandHandler<GetTickerPriceCommand>)this).Create(payload, logger),
            "binance-avg-price" => ((ICommandHandler<GetAveragePriceCommand>)this).Create(payload, logger),
            "binance-convert-get-quote" => ((ICommandHandler<GetConvertQuoteCommand>)this).Create(payload, logger),
            "binance-convert-accept-quote" => ((ICommandHandler<AcceptConvertQuoteCommand>)this).Create(payload, logger),
            "binance-convert-order-status" => ((ICommandHandler<GetConvertOrderStatusCommand>)this).Create(payload, logger),
            "binance-convert-trade-flow" => ((ICommandHandler<GetConvertTradeFlowCommand>)this).Create(payload, logger),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'")
        };
    }

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}

