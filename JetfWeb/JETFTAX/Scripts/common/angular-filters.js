/**
 * 共用的 AngularJS Filters
 * 包含日期格式化等常用過濾器
 */
(function() {
    'use strict';

    // 創建 common filters 模組
    angular.module('commonFilters', [])
        
        .filter('customDate', function () {
            return (jsonDate, format) => {

                if (jsonDate == null)
                    return "";

                var formatPattern = format ? format : 'YYYY/MM/DD';

                // 若日期為空或無效，直接回傳空字串
                if (!jsonDate || !moment(jsonDate).isValid()) {
                    return '';
                }

                return moment(jsonDate).format(formatPattern);
            };
        })

        .filter('customTime', function () {
            return (jsonDate, format) => {

                var formatPattern = format ? format : 'HH:mm:ss';

                // 若日期為空或無效，直接回傳空字串
                if (!jsonDate || !moment(jsonDate).isValid()) {
                    return '';
                }

                return moment(jsonDate).format(formatPattern);
            };
        })

        .filter('customDateTime', function () {
            return (jsonDate, format) => {

                var formatPattern = format ? format : 'YYYY/MM/DD HH:mm:ss';

                // 若日期為空或無效，直接回傳空字串
                if (!jsonDate || !moment(jsonDate).isValid()) {
                    return '';
                }

                return moment(jsonDate).format(formatPattern);
            };
        })

        /**
         * formatDateStr Filter
         * 格式化日期字串 (例如: 20231225 -> 2023/12/25)
         */
        .filter('formatDateStr', function() {
            return function(dateStr, sep) {
                sep = sep || '/';
                if (!dateStr || dateStr.length !== 8) return '';
                
                var year = dateStr.substring(0, 4);
                var month = dateStr.substring(4, 6);
                var day = dateStr.substring(6, 8);
                return year + sep + month + sep + day;
            };
        })

        /**
         * dateToInput Filter
         * 轉換日期為 HTML date input 可用格式 (YYYY-MM-DD)
         */
        .filter('dateToInput', function() {
            return function(dateValue) {
                if (!dateValue) return '';
                
                var date;
                if (typeof dateValue === 'string' && dateValue.indexOf('/Date(') !== -1) {
                    var timestamp = parseInt(dateValue.replace(/\/Date\((\d+)\)\//, '$1'), 10);
                    date = moment(timestamp);
                } else {
                    date = moment(dateValue);
                }
                
                return date.isValid() ? date.format('YYYY-MM-DD') : '';
            };
        })

        /**
         * currency Filter
         * 格式化貨幣顯示
         */
        .filter('currency', function() {
            return function(amount, symbol) {
                if (amount === null || amount === undefined || isNaN(amount)) return '';
                
                symbol = symbol || '$';
                var formatted = parseFloat(amount).toLocaleString('zh-TW', {
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 2
                });
                
                return symbol + formatted;
            };
        })

        /**
         * truncate Filter
         * 截斷字串並加上省略符號
         */
        .filter('truncate', function() {
            return function(text, length, suffix) {
                if (!text) return '';
                
                length = length || 50;
                suffix = suffix || '...';
                
                if (text.length <= length) {
                    return text;
                }
                
                return text.substring(0, length - suffix.length) + suffix;
            };
        })

        /**
         * range Filter
         * 產生數字範圍的陣列，用於分頁等場景
         */
        .filter('range', function() {
            return function(input, start, end) {
                start = parseInt(start);
                end = parseInt(end);
                var direction = (start <= end) ? 1 : -1;
                
                while (start !== end) {
                    input.push(start);
                    start += direction;
                }
                input.push(end);
                
                return input;
            };
        });

})();