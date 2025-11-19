// Legacy Home Page JavaScript - Adapted for new project
// Based on FAXER.PORTAL/Views/Home/Index.cshtml

// Wait for jQuery to be available
(function() {
    'use strict';
    
    // Function to initialize when jQuery is ready
    function initializeWhenReady() {
        if (typeof jQuery !== 'undefined' && typeof $ !== 'undefined') {
            init();
        } else {
            // Retry after a short delay
            setTimeout(initializeWhenReady, 50);
        }
    }

    var selectedSendingCountry = "GB";
    var selectedReceivingCountry = "";
    var selectedReceivingCurrency = "";
    var SendingCountry = "";
    var ReceivingCountry = "";
    var selectedTransferMethod = "";
    var transferMethodvalue = 4; // Default to BankAccount
    var validationResult = null;

    function init() {
        var $ = jQuery; // Use jQuery explicitly
        
        // Initialize on page load
        $(document).ready(function() {
            selectedReceivingCountry = $("#receivingCountry").attr('data-country') || "NG";
            selectedReceivingCurrency = $("#receivingCountry").attr('data-currency') || "NGN";
            transferMethodvalue = $("#transferMethod input[type='radio']:checked").val() || 4;
            
            $("#SendingAmount").val(3);
            CalculateSummary(false);
            
            // Focus handlers
            $("#SendingAmount").focus(function () {
                if ($("#SendingAmount").val() == 3 || $("#SendingAmount").val() == 0) {
                    $("#SendingAmount").val("");
                    $("#ReceivingAmount").val("");
                }
            });

            $("#ReceivingAmount").focus(function () {
                if ($("#SendingAmount").val() == 3 || $("#SendingAmount").val() == 0) {
                    $("#SendingAmount").val("");
                    $("#ReceivingAmount").val("");
                }
            });
        });
    }

    // Make functions globally available
    window.OnReceivingCountryChange = function(val, cur) {
        if (typeof jQuery === 'undefined') {
            console.error('jQuery not loaded');
            return;
        }
        var $ = jQuery;
        if (typeof event !== 'undefined' && event && event.currentTarget) {
            $("#receivingCountry").text($($(event.currentTarget).find('span')[0]).text());
        }
        $("#receivingCountry").attr('data-currency', cur);
        selectedReceivingCountry = val;
        $("#SendingAmount").val(3);
        SendingCountry = selectedSendingCountry;
        ReceivingCountry = selectedReceivingCountry;
        var ReceivingCurrency = $("#receivingCountry").attr('data-currency');
        CalculateSummary(false);
    };

    window.OnSendingCountryChange = function(val, cur) {
        if (typeof jQuery === 'undefined') {
            console.error('jQuery not loaded');
            return;
        }
        var $ = jQuery;
        if (typeof event !== 'undefined' && event && event.currentTarget) {
            $("#sendingCountry").text($($(event.currentTarget).find('span')[0]).text());
        }
        $("#sendingCountry").attr('data-currency', cur);
        selectedSendingCountry = val;
        $("#SendingAmount").val(3);
        SendingCountry = selectedSendingCountry;
        ReceivingCountry = selectedReceivingCountry;
        var ReceivingCurrency = $("#receivingCountry").attr('data-currency');
        CalculateSummary(false);
    };

    window.CalculateSummary = function(IsReceivingAmount) {
        if (typeof jQuery === 'undefined') {
            console.error('jQuery not loaded');
            return;
        }
        var $ = jQuery;
        SendingCountry = selectedSendingCountry;
        ReceivingCountry = selectedReceivingCountry;
        var ReceivingCurrency = $("#receivingCountry").attr('data-currency');
        GetPaymentSummary(IsReceivingAmount);
    };

    window.GetPaymentSummary = function(IsReceivingAmount) {
        if (typeof jQuery === 'undefined') {
            console.error('jQuery not loaded');
            return;
        }
        var $ = jQuery;
        var SendingAmount = $("#SendingAmount").val();
        var ReceivingAmount = $("#ReceivingAmount").val();
        var SendingCurrency = $("#sendingCountry").attr('data-currency');
        var ReceivingCurrency = $("#receivingCountry").attr('data-currency');
        SendingCountry = selectedSendingCountry;
        ReceivingCountry = selectedReceivingCountry;

        try {
            transferMethodvalue = $("#transferMethod input[type='radio']:checked").val();
        } catch (e) {
            transferMethodvalue = 4;
        }

        var data = {
            SendingAmount: parseFloat(SendingAmount) || 0,
            ReceivingAmount: parseFloat(ReceivingAmount) || 0,
            SendingCurrency: SendingCurrency,
            ReceivingCurrency: ReceivingCurrency,
            SendingCountry: SendingCountry,
            ReceivingCountry: ReceivingCountry,
            IsReceivingAmount: IsReceivingAmount,
            TransferMethod: parseInt(transferMethodvalue) || 4
        };

        // Use standard jQuery AJAX instead of Riddha.ajax
        $.ajax({
            url: '/Home/GetTransferSummary',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(result) {
                // Update exchange rate
                $('[name="ExchangeRate"]').text(result.exchangeRate ? result.exchangeRate.toFixed(2) : '0.00');
                $('[name="SendingCurrency"]').text(result.sendingCurrency || 'GBP');
                $('[name="ReceivingCurrency"]').text(result.receivingCurrency || 'NGN');
                $('[name="SendingCurrencySymbol"]').text(result.sendingCurrencySymbol || '£');
                
                // Update amounts
                if (result.receivingAmount !== undefined && result.receivingAmount !== null) {
                    $("#ReceivingAmount").val(result.receivingAmount.toFixed(2));
                }
                if (result.sendingAmount !== undefined && result.sendingAmount !== null) {
                    $("#SendingAmount").val(result.sendingAmount.toFixed(2));
                }
                
                // Update fee display
                var fee = result.fee || result.actualFee || 0;
                $('[name="Fee"]').text(fee.toFixed(2));
                
                if (result.isIntroductoryFee && fee == 0) {
                    $("#IsIntroductoryFee, #IsIntroductoryFee2").show();
                    $("#IsNotIntroductoryFee, #IsNotIntroductoryFee2").hide();
                } else {
                    $("#IsIntroductoryFee, #IsIntroductoryFee2").hide();
                    $("#IsNotIntroductoryFee, #IsNotIntroductoryFee2").show();
                }
                
                // Update validation
                validationResult = result.isValid || { data: true, message: '' };
                if (!validationResult.data) {
                    $("#ValidationResult").text(validationResult.message || 'Invalid transaction');
                    $("#ValidationResultdiv").show();
                } else {
                    $("#ValidationResult").text('');
                    $("#ValidationResultdiv").hide();
                }
            },
            error: function(xhr, status, error) {
                console.error('Error calculating summary:', error);
                $("#ValidationResult").text('Error calculating transfer summary. Please try again.');
                $("#ValidationResultdiv").show();
            }
        });
    };

    window.TransferNow = function() {
        if (typeof jQuery === 'undefined') {
            console.error('jQuery not loaded');
            return;
        }
        var $ = jQuery;
        if (validationResult && !validationResult.data) {
            $("#ValidationResult").text(validationResult.message);
            $("#ValidationResultdiv").show();
            return;
        }
        $("#ValidationResultdiv").hide();

        var sendingAmount = $("#SendingAmount").val();
        if (sendingAmount <= 0) {
            $("#validationMsg").text("Enter an amount");
            $("#validationMsg").show();
            return;
        } else if (sendingAmount > 50000) {
            $("#validationMsg").text("Please enter send amount less than or equal to GBP 50,000");
            $("#validationMsg").show();
            return;
        } else {
            $("#validationMsg").hide();
        }
        
        var $selectedRadio = $("#transferMethod input[type='radio']:checked");
        var transferMethodValue = $selectedRadio.val();
        
        // Get currency and country information
        var receivingAmount = $("#ReceivingAmount").val() || 0;
        var sendingCurrency = $("#sendingCountry").attr('data-currency') || 'GBP';
        var receivingCurrency = $("#receivingCountry").attr('data-currency') || 'NGN';
        
        // Get country codes from data attributes or fallback to selected variables
        var sendingCountryCode = $("#sendingCountry").attr('data-country-code') || selectedSendingCountry || 'GB';
        var receivingCountryCode = $("#receivingCountry").attr('data-country-code') || selectedReceivingCountry || 'NG';

        // Bank Account (value 4) - redirect to SenderBankAccountDeposit/Index
        if (transferMethodValue === '4') {
            var bankDepositUrl = '/SenderBankAccountDeposit/Index?'
                + 'SendingAmount=' + encodeURIComponent(sendingAmount)
                + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
                + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
                + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
                + '&SendingCountry=' + encodeURIComponent(sendingCountryCode)
                + '&ReceivingCountry=' + encodeURIComponent(receivingCountryCode);
            window.location.href = bankDepositUrl;
            return;
        }

        // Mobile Wallet - redirect to MobileMoneyTransfer controller
        if (transferMethodValue === '3') {
            var mobileUrl = '/MobileMoneyTransfer?'
                + 'SendingAmount=' + encodeURIComponent(sendingAmount)
                + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
                + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
                + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
                + '&SendingCountry=' + encodeURIComponent(sendingCountryCode)
                + '&ReceivingCountry=' + encodeURIComponent(receivingCountryCode)
                + '&CountryCode=' + encodeURIComponent(receivingCountryCode);

            window.location.href = mobileUrl;
            return;
        }

        // Cash Pickup (value 1) - redirect to SenderCashPickUp controller
        if (transferMethodValue === '1') {
            var cashPickupUrl = '/SenderCashPickUp/Index?'
                + 'SendingAmount=' + encodeURIComponent(sendingAmount)
                + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
                + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
                + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
                + '&SendingCountry=' + encodeURIComponent(sendingCountryCode)
                + '&ReceivingCountry=' + encodeURIComponent(receivingCountryCode)
                + '&CountryCode=' + encodeURIComponent(receivingCountryCode);
            window.location.href = cashPickupUrl;
            return;
        }

        var url = '/SendMoney/';
        switch (transferMethodValue) {
            case '2':
                url += 'KiiBank';
                break;
            default:
                // Default to Bank Account redirect
                var defaultBankDepositUrl = '/SenderBankAccountDeposit/Index?'
                    + 'SendingAmount=' + encodeURIComponent(sendingAmount)
                    + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
                    + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
                    + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
                    + '&SendingCountry=' + encodeURIComponent(sendingCountryCode)
                    + '&ReceivingCountry=' + encodeURIComponent(receivingCountryCode);
                window.location.href = defaultBankDepositUrl;
                return;
        }

        url += '?amount=' + sendingAmount + '&sendingCountry=' + sendingCountryCode + '&receivingCountry=' + receivingCountryCode;
        window.location.href = url;
    };

    // Start initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeWhenReady);
    } else {
        initializeWhenReady();
    }
})();
