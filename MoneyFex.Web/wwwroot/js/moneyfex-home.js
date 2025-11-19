// MoneyFex Home Page JavaScript - Optimized

// Global variables for selected countries/currencies
var selectedSendingCountry = 'GB';
var selectedReceivingCountry = 'NG';
var selectedReceivingCurrency = 'NGN';
var validationResult = { data: true, message: '' };

$(document).ready(function() {
    // Initialize default values from data attributes (set in view)
    var sendingCountryEl = $('#sendingCountry');
    var receivingCountryEl = $('#receivingCountry');
    
    if (sendingCountryEl.length && sendingCountryEl.data('country-code')) {
        selectedSendingCountry = sendingCountryEl.data('country-code');
    }
    
    if (receivingCountryEl.length && receivingCountryEl.data('country-code')) {
        selectedReceivingCountry = receivingCountryEl.data('country-code');
        selectedReceivingCurrency = receivingCountryEl.data('currency') || 'NGN';
    }
    
    // Load countries from API
    loadCountriesFromAPI();
    
    // Currency dropdown toggle
    $('.mf-currency').on('click', function(e) {
        e.stopPropagation();
        e.preventDefault();
        var dropdown = $(this).next('.currency-dropdown');
        $('.currency-dropdown').not(dropdown).addClass('closed');
        dropdown.toggleClass('closed');
    });

    // Close dropdown when clicking outside
    $(document).on('click', function(e) {
        if (!$(e.target).closest('.mf-currency, .currency-dropdown').length) {
            $('.currency-dropdown').addClass('closed');
        }
    });

    // Currency search filter for sending currency
    $('#searchCurrency').on('keyup', function() {
        var value = $(this).val().toLowerCase();
        $('#currencylist li').filter(function() {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Currency search filter for receiving currency
    $('#searchCurrency1').on('keyup', function() {
        var value = $(this).val().toLowerCase();
        $('#currencylist1 li').filter(function() {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Number input validation
    $('input[type="number"]').on('keyup', function() {
        var amount = this.value;
        if (amount.split(".").length > 1) {
            var regexp = /^\d+\.\d{0,2}$/;
        } else {
            var regexp = /^\d+$/;
        }
        if (!regexp.test(amount) && amount !== '') {
            this.value = '';
        }
    });

    // Clear placeholder on focus
    $('#SendingAmount, #ReceivingAmount').on('focus', function() {
        if ($(this).val() == 3 || $(this).val() == 0) {
            $(this).val('');
        }
    });
});

// Load countries from API
function loadCountriesFromAPI() {
    var apiBaseUrl = window.location.origin;
    
    $.getJSON(apiBaseUrl + '/api/countries')
        .done(function(countries) {
            console.log('Countries loaded from API:', countries);
            
            // Populate sending countries dropdown
            var sendingList = $('#currencylist');
            sendingList.empty();
            
            countries.forEach(function(country) {
                var li = $('<li>')
                    .addClass('currency-dropdown_option')
                    .attr('onclick', "OnSendingCountryChange('" + country.countryCode + "', '" + country.currency + "', '" + country.countryName.replace(/'/g, "\\'") + "')");
                
                var flag = $('<div>')
                    .addClass('currency-dropdown_option_flag flag-icon flag-icon-' + country.countryCode.toLowerCase());
                
                var text = $('<span>')
                    .addClass('currency-dropdown_option_text')
                    .html(country.currency + ' &nbsp;&nbsp; from ' + country.countryName);
                
                li.append(flag).append(text);
                sendingList.append(li);
            });
            
            // Populate receiving countries dropdown
            var receivingList = $('#currencylist1');
            receivingList.empty();
            
            countries.forEach(function(country) {
                var li = $('<li>')
                    .addClass('currency-dropdown_option')
                    .attr('onclick', "OnReceivingCountryChange('" + country.countryCode + "', '" + country.currency + "', '" + country.countryName.replace(/'/g, "\\'") + "')");
                
                var flag = $('<div>')
                    .addClass('currency-dropdown_option_flag flag-icon flag-icon-' + country.countryCode.toLowerCase());
                
                var text = $('<span>')
                    .addClass('currency-dropdown_option_text')
                    .html(country.currency + ' &nbsp;&nbsp; ' + country.countryName);
                
                li.append(flag).append(text);
                receivingList.append(li);
            });
            
            // Set default selected countries
            var defaultSendingCountry = countries.find(c => c.countryCode === selectedSendingCountry) || countries[0];
            var defaultReceivingCountry = countries.find(c => c.countryCode === selectedReceivingCountry) || countries.find(c => c.countryCode === 'NG') || countries[0];
            
            if (defaultSendingCountry) {
                $('#sendingCountry').html(defaultSendingCountry.currency + ' &nbsp;&nbsp; from ' + defaultSendingCountry.countryName);
                $('#sendingCountry').attr('data-currency', defaultSendingCountry.currency);
                selectedSendingCountry = defaultSendingCountry.countryCode;
            }
            
            if (defaultReceivingCountry) {
                $('#receivingCountry').html(defaultReceivingCountry.currency + ' &nbsp;&nbsp; ' + defaultReceivingCountry.countryName);
                $('#receivingCountry').attr('data-currency', defaultReceivingCountry.currency);
                selectedReceivingCountry = defaultReceivingCountry.countryCode;
                selectedReceivingCurrency = defaultReceivingCountry.currency;
            }
        })
        .fail(function(xhr, status, error) {
            console.error('Failed to load countries from API:', error);
            // Fallback: use server-side rendered countries if API fails
            console.log('Using server-side rendered countries as fallback');
        });
}

// Country change handlers
function OnSendingCountryChange(countryCode, currency, countryName) {
    if (!countryName) {
        // Fallback if countryName is not provided
        countryName = $('#currencylist li').filter(function() {
            return $(this).find('.currency-dropdown_option_text').text().includes(currency);
        }).first().find('.currency-dropdown_option_text').text().split('from ')[1] || '';
    }
    
    $('#sendingCountry').html(currency + ' &nbsp;&nbsp; from ' + (countryName || ''));
    $('#sendingCountry').attr('data-currency', currency);
    selectedSendingCountry = countryCode;
    
    // Only set default value if amount is 0 or empty
    var currentAmount = parseFloat($('#SendingAmount').val()) || 0;
    if (currentAmount === 0 || currentAmount === 3) {
        $('#SendingAmount').val('');
    }
    
    $('.currency-dropdown').addClass('closed');
    GetPaymentSummary(false);
}

function OnReceivingCountryChange(countryCode, currency, countryName) {
    if (!countryName) {
        // Fallback if countryName is not provided
        countryName = $('#currencylist1 li').filter(function() {
            return $(this).find('.currency-dropdown_option_text').text().includes(currency);
        }).first().find('.currency-dropdown_option_text').text().split(currency + ' &nbsp;&nbsp; ')[1] || '';
    }
    
    $('#receivingCountry').html(currency + ' &nbsp;&nbsp; ' + (countryName || ''));
    $('#receivingCountry').attr('data-currency', currency);
    selectedReceivingCountry = countryCode;
    selectedReceivingCurrency = currency;
    
    // Only set default value if amount is 0 or empty
    var currentAmount = parseFloat($('#SendingAmount').val()) || 0;
    if (currentAmount === 0 || currentAmount === 3) {
        $('#SendingAmount').val('');
    }
    
    $('.currency-dropdown').addClass('closed');
    GetPaymentSummary(false);
}

// Get payment summary
function GetPaymentSummary(isReceivingAmount) {
    var sendingAmount = parseFloat($('#SendingAmount').val()) || 0;
    var receivingAmount = parseFloat($('#ReceivingAmount').val()) || 0;
    var sendingCurrency = $('#sendingCountry').attr('data-currency');
    var receivingCurrency = $('#receivingCountry').attr('data-currency');
    var transferMethod = $('input[name="radio"]:checked').val() || 4;

    var data = {
        SendingAmount: sendingAmount,
        ReceivingAmount: receivingAmount,
        SendingCurrency: sendingCurrency,
        ReceivingCurrency: receivingCurrency,
        SendingCountry: selectedSendingCountry,
        ReceivingCountry: selectedReceivingCountry,
        IsReceivingAmount: isReceivingAmount,
        TransferMethod: parseInt(transferMethod)
    };

    // Show loading indicator
    $('#ValidationResultdiv').hide();
    $('[name="ExchangeRate"]').text('...');
    $('[name="Fee"]').text('...');

    $.ajax({
        url: '/Home/GetTransferSummary',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function(result) {
            UpdateSummaryDisplay(result);
        },
        error: function(xhr, status, error) {
            console.error('Error calculating summary:', error);
            $('#ValidationResult').text('Error calculating transfer summary. Please try again.');
            $('#ValidationResultdiv').show();
            $('[name="ExchangeRate"]').text('0.00');
            $('[name="Fee"]').text('0.00');
        }
    });
}

// Update summary display
function UpdateSummaryDisplay(result) {
    // Update exchange rate
    $('[name="ExchangeRate"]').text(result.exchangeRate ? result.exchangeRate.toFixed(2) : '0.00');
    $('[name="SendingCurrency"]').text(result.sendingCurrency || 'GBP');
    $('[name="ReceivingCurrency"]').text(result.receivingCurrency || 'NGN');
    $('[name="SendingCurrencySymbol"]').text(result.sendingCurrencySymbol || '£');
    
    // Update amounts
    if (result.receivingAmount !== undefined && result.receivingAmount !== null) {
        $('#ReceivingAmount').val(result.receivingAmount.toFixed(2));
    }
    if (result.sendingAmount !== undefined && result.sendingAmount !== null) {
        $('#SendingAmount').val(result.sendingAmount.toFixed(2));
    }
    
    // Update fee display
    var fee = result.fee || result.actualFee || 0;
    $('[name="Fee"]').text(fee.toFixed(2));
    
    if (result.isIntroductoryFee && fee == 0) {
        $('#IsIntroductoryFee, #IsIntroductoryFee2').show();
        $('#IsNotIntroductoryFee, #IsNotIntroductoryFee2').hide();
    } else {
        $('#IsIntroductoryFee, #IsIntroductoryFee2').hide();
        $('#IsNotIntroductoryFee, #IsNotIntroductoryFee2').show();
    }
    
    // Update validation
    validationResult = result.isValid || { data: true, message: '' };
    if (!validationResult.data) {
        $('#ValidationResult').text(validationResult.message || 'Invalid transaction');
        $('#ValidationResultdiv').show();
    } else {
        $('#ValidationResult').text('');
        $('#ValidationResultdiv').hide();
    }
}

// Transfer now
function TransferNow() {
    if (validationResult && !validationResult.data) {
        $('#ValidationResult').text(validationResult.message);
        $('#ValidationResultdiv').show();
        return;
    }
    
    $('#ValidationResultdiv').hide();
    
    var sendingAmount = parseFloat($('#SendingAmount').val()) || 0;
    if (sendingAmount <= 0) {
        $('#validationMsg').text('Enter an amount');
        $('#validationMsg').show();
        return;
    } else if (sendingAmount > 50000) {
        $('#validationMsg').text('Please enter send amount less than or equal to GBP 50,000');
        $('#validationMsg').show();
        return;
    } else {
        $('#validationMsg').hide();
    }
    
    var transferMethod = $('input[name="radio"]:checked').attr('id');
    var sendingCurrency = $('#sendingCountry').attr('data-currency') || 'GBP';
    var receivingCurrency = $('#receivingCountry').attr('data-currency') || 'NGN';
    var receivingAmount = parseFloat($('#ReceivingAmount').val()) || 0;
    
    // Direct redirect for Cash Pickup
    if (transferMethod === 'CashPickup') {
        var cashPickupUrl = '/SenderCashPickUp/Index?'
            + 'SendingAmount=' + encodeURIComponent(sendingAmount)
            + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
            + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
            + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
            + '&SendingCountry=' + encodeURIComponent(selectedSendingCountry)
            + '&ReceivingCountry=' + encodeURIComponent(selectedReceivingCountry)
            + '&CountryCode=' + encodeURIComponent(selectedReceivingCountry);
        window.location.href = cashPickupUrl;
        return;
    }

    // Direct redirect for Bank Account - same as TransferMoneyNow page
    if (transferMethod === 'BankAccount') {
        // Redirect to SenderBankAccountDeposit/Index for bank deposit with all parameters
        var bankDepositUrl = '/SenderBankAccountDeposit/Index?'
            + 'SendingAmount=' + encodeURIComponent(sendingAmount)
            + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
            + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
            + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
            + '&SendingCountry=' + encodeURIComponent(selectedSendingCountry)
            + '&ReceivingCountry=' + encodeURIComponent(selectedReceivingCountry);
        window.location.href = bankDepositUrl;
        return;
    }
    
    // Direct redirect for Mobile Wallet
    if (transferMethod === 'MobileWallet') {
        var mobileUrl = '/MobileMoneyTransfer?'
            + 'CountryCode=' + encodeURIComponent(selectedReceivingCountry)
            + '&SendingAmount=' + encodeURIComponent(sendingAmount)
            + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
            + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
            + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
            + '&SendingCountry=' + encodeURIComponent(selectedSendingCountry)
            + '&ReceivingCountry=' + encodeURIComponent(selectedReceivingCountry);
        window.location.href = mobileUrl;
        return;
    }
    
    // Direct redirect for KiiBank
    if (transferMethod === 'KiiBank') {
        var kiiBankUrl = '/KiiBankTransfer/AccountDetails?'
            + 'SendingAmount=' + encodeURIComponent(sendingAmount)
            + '&ReceivingAmount=' + encodeURIComponent(receivingAmount)
            + '&SendingCurrency=' + encodeURIComponent(sendingCurrency)
            + '&ReceivingCurrency=' + encodeURIComponent(receivingCurrency)
            + '&SendingCountry=' + encodeURIComponent(selectedSendingCountry)
            + '&ReceivingCountry=' + encodeURIComponent(selectedReceivingCountry);
        window.location.href = kiiBankUrl;
        return;
    }
    
    var transferMethodMap = {
        'BankAccount': 'BankDeposit'
    };
    
    var action = transferMethodMap[transferMethod] || 'BankDeposit';
    window.location.href = '/SendMoney/' + action + '?amount=' + sendingAmount + 
        '&sendingCountry=' + selectedSendingCountry + 
        '&receivingCountry=' + selectedReceivingCountry;
}

// Auto-calculate on input
$('#SendingAmount').on('keyup', function() {
    GetPaymentSummary(false);
});

$('#ReceivingAmount').on('keyup', function() {
    GetPaymentSummary(true);
});

