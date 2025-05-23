﻿/**
 * Finance System - Financings Page
 * Scripts específicos para a página de financiamentos
 */

// Namespace global para o sistema
var FinanceSystem = FinanceSystem || {};
FinanceSystem.Pages = FinanceSystem.Pages || {};

// Módulo Financings
FinanceSystem.Pages.Financings = (function () {
    /**
     * Inicializa a página de financiamentos
     */
    function initialize() {
        // Determina qual view está ativa
        const isFormView = document.querySelector('form[asp-action="Create"], form[asp-action="Edit"]');
        const isListView = document.querySelector('.financings-list');
        const isDetailsView = document.querySelector('.financing-details-container');
        const isSimulationView = document.querySelector('form[asp-action="Simulate"]');

        if (isFormView) {
            initializeFinancingForm();
        }

        if (isListView) {
            initializeFinancingsList();
        }

        if (isDetailsView) {
            initializeFinancingDetails();
        }

        if (isSimulationView) {
            initializeFinancingSimulation();
        }
    }

    /**
     * Inicializa formulário de financiamento
     */
    function initializeFinancingForm() {
        const form = document.querySelector('form[asp-action="Create"], form[asp-action="Edit"]');
        if (!form) return;

        // Inicializa máscaras para campos monetários
        initializeMoneyMasks();

        // Validação de campos específicos
        initializeFieldValidations();

        // Configura calculadora de parcelas
        initializeInstallmentCalculator();

        // Configura validação do formulário
        setupFormValidation(form);
    }

    /**
     * Inicializa máscaras para valores monetários
     */
    function initializeMoneyMasks() {
        // Usa o módulo Financial se disponível
        if (FinanceSystem.Modules && FinanceSystem.Modules.Financial) {
            FinanceSystem.Modules.Financial.initializeMoneyMask('#TotalAmount');
            FinanceSystem.Modules.Financial.initializeMoneyMask('#InstallmentValue');
            return;
        }

        // Fallback para jQuery Mask se disponível
        if (typeof $.fn.mask !== 'undefined') {
            $('#TotalAmount, #InstallmentValue').mask('#.##0,00', { reverse: true });
            $('#InterestRate').mask('##0,00%', { reverse: true });
        } else {
            // Implementação manual se as bibliotecas não estiverem disponíveis
            const moneyInputs = document.querySelectorAll('#TotalAmount, #InstallmentValue');
            moneyInputs.forEach(input => {
                input.addEventListener('input', function (e) {
                    formatCurrencyInput(this);
                });

                // Formata valor inicial se existir
                if (input.value) {
                    formatCurrencyInput(input);
                }
            });

            // Campo de taxa de juros
            const interestRateInput = document.getElementById('InterestRate');
            if (interestRateInput) {
                interestRateInput.addEventListener('input', function (e) {
                    formatPercentInput(this);
                });

                // Formata valor inicial se existir
                if (interestRateInput.value) {
                    formatPercentInput(interestRateInput);
                }
            }
        }
    }

    /**
     * Formata campo de entrada monetária
     * @param {HTMLElement} input - Campo a ser formatado
     */
    function formatCurrencyInput(input) {
        // Preserva a posição do cursor
        const cursorPosition = input.selectionStart;
        const inputLength = input.value.length;

        // Remove caracteres não numéricos, exceto vírgula e ponto
        let value = input.value.replace(/[^\d.,]/g, '');

        // Converte para o formato brasileiro (vírgula como separador decimal)
        value = value.replace(/\D/g, '');
        if (value === '') {
            input.value = '';
            return;
        }

        value = (parseFloat(value) / 100).toFixed(2);
        input.value = value.replace('.', ',');

        // Ajusta a posição do cursor se necessário
        const newLength = input.value.length;
        const newPosition = cursorPosition + (newLength - inputLength);
        if (newPosition >= 0) {
            input.setSelectionRange(newPosition, newPosition);
        }
    }

    /**
     * Formata campo de entrada percentual
     * @param {HTMLElement} input - Campo a ser formatado
     */
    function formatPercentInput(input) {
        // Remove caracteres não numéricos, exceto vírgula e ponto
        let value = input.value.replace(/[^\d.,]/g, '');

        // Se tem vírgula, limita a duas casas decimais
        if (value.includes(',')) {
            const parts = value.split(',');
            if (parts[1].length > 2) {
                parts[1] = parts[1].substring(0, 2);
                value = parts.join(',');
            }
        }

        // Adiciona o símbolo de percentual se não estiver presente
        if (!input.value.includes('%') && value) {
            value += '%';
        }

        input.value = value;
    }

    /**
     * Inicializa validações de campos específicos
     */
    function initializeFieldValidations() {
        // Validação de prazo
        const termMonthsField = document.getElementById('TermMonths');
        if (termMonthsField) {
            termMonthsField.addEventListener('change', function () {
                const value = parseInt(this.value, 10);

                if (isNaN(value) || value <= 0 || value > 600) {
                    showFieldError(this, 'O prazo deve estar entre 1 e 600 meses');
                } else {
                    clearFieldError(this);
                }
            });
        }

        // Validação de taxa de juros
        const interestRateField = document.getElementById('InterestRate');
        if (interestRateField) {
            interestRateField.addEventListener('change', function () {
                let value = this.value.replace(/[^\d.,]/g, '').replace(',', '.');
                const rate = parseFloat(value);

                if (isNaN(rate) || rate < 0 || rate > 100) {
                    showFieldError(this, 'A taxa de juros deve estar entre 0 e 100%');
                } else {
                    clearFieldError(this);
                }
            });
        }
    }

    /**
     * Inicializa calculadora de parcelas
     */
    function initializeInstallmentCalculator() {
        const totalAmountInput = document.getElementById('TotalAmount');
        const interestRateInput = document.getElementById('InterestRate');
        const termMonthsInput = document.getElementById('TermMonths');
        const installmentValueInput = document.getElementById('InstallmentValue');
        const calculationButton = document.getElementById('calculateButton');
        const amortizationSystemSelect = document.getElementById('Type');

        if (calculationButton && totalAmountInput && interestRateInput && termMonthsInput && installmentValueInput) {
            calculationButton.addEventListener('click', function (e) {
                e.preventDefault();
                calculateInstallment();
            });
        }

        // Recalcula automaticamente quando os valores mudam
        if (totalAmountInput && interestRateInput && termMonthsInput && installmentValueInput) {
            totalAmountInput.addEventListener('change', calculateInstallment);
            interestRateInput.addEventListener('change', calculateInstallment);
            termMonthsInput.addEventListener('change', calculateInstallment);

            if (amortizationSystemSelect) {
                amortizationSystemSelect.addEventListener('change', calculateInstallment);
            }
        }
    }

    /**
     * Calcula valor de parcela
     */
    function calculateInstallment() {
        const totalAmountInput = document.getElementById('TotalAmount');
        const interestRateInput = document.getElementById('InterestRate');
        const termMonthsInput = document.getElementById('TermMonths');
        const installmentValueInput = document.getElementById('InstallmentValue');
        const resultElement = document.getElementById('calculationResult');
        const typeSelect = document.getElementById('Type');

        if (!totalAmountInput || !interestRateInput || !termMonthsInput) return;

        // Obter valores dos campos
        let totalAmount = parseCurrency(totalAmountInput.value);
        let interestRate = parsePercent(interestRateInput.value);
        let termMonths = parseInt(termMonthsInput.value);
        let type = typeSelect ? typeSelect.value : 'PRICE';

        if (isNaN(totalAmount) || isNaN(interestRate) || isNaN(termMonths) || termMonths <= 0) {
            if (resultElement) {
                resultElement.textContent = 'Preencha todos os campos corretamente';
            }
            return;
        }

        // Converter taxa de juros anual para mensal
        const monthlyRate = interestRate / 12;
        let installmentValue = 0;
        let message = '';

        // Usar o módulo Financial para cálculos se disponível
        if (FinanceSystem.Modules && FinanceSystem.Modules.Financial) {
            // Obter a primeira parcela do array de parcelas
            const installments = FinanceSystem.Modules.Financial.calculateInstallments(
                totalAmount, interestRate * 100, termMonths, type
            );

            if (installments && installments.length > 0) {
                installmentValue = installments[0].value;

                if (type === 'SAC') {
                    message = `
                        Primeira parcela: R$ ${formatNumber(installments[0].value)}<br>
                        Última parcela: R$ ${formatNumber(installments[installments.length - 1].value)}<br>
                        Amortização mensal: R$ ${formatNumber(installments[0].principal)}
                    `;
                }
            }
        } else {
            // Cálculos manuais
            if (type === 'PRICE') {
                // Sistema Price (parcelas fixas)
                if (interestRate === 0) {
                    installmentValue = totalAmount / termMonths;
                } else {
                    installmentValue = totalAmount * (monthlyRate * Math.pow(1 + monthlyRate, termMonths)) / (Math.pow(1 + monthlyRate, termMonths) - 1);
                }
            } else if (type === 'SAC') {
                // Sistema SAC (amortizações fixas)
                const amortization = totalAmount / termMonths;
                const firstInterest = totalAmount * monthlyRate;
                const firstInstallment = amortization + firstInterest;

                const lastPrincipal = totalAmount - (amortization * (termMonths - 1));
                const lastInterest = lastPrincipal * monthlyRate;
                const lastInstallment = lastPrincipal + lastInterest;

                installmentValue = firstInstallment;

                message = `
                    Primeira parcela: R$ ${formatNumber(firstInstallment)}<br>
                    Última parcela: R$ ${formatNumber(lastInstallment)}<br>
                    Amortização mensal: R$ ${formatNumber(amortization)}
                `;
            }
        }

        // Atualiza o campo de valor da parcela
        if (installmentValueInput) {
            installmentValueInput.value = formatNumber(installmentValue);
        }

        // Exibe resultados adicionais se houver elemento para isso
        if (resultElement) {
            if (message) {
                resultElement.innerHTML = message;
            } else {
                resultElement.textContent = `Valor da parcela: R$ ${formatNumber(installmentValue)}`;
            }
        }
    }

    /**
     * Configura validação do formulário
     * @param {HTMLFormElement} form - Formulário
     */
    function setupFormValidation(form) {
        if (!form) return;

        // Usa o módulo de validação se disponível
        if (FinanceSystem.Validation && FinanceSystem.Validation.setupFormValidation) {
            FinanceSystem.Validation.setupFormValidation(form, validateFinancingForm);
        } else {
            form.addEventListener('submit', function (event) {
                if (!validateFinancingForm(event)) {
                    event.preventDefault();
                    event.stopPropagation();
                }
            });
        }
    }

    /**
     * Valida o formulário de financiamento
     * @param {Event} event - Evento de submissão
     * @returns {boolean} - Resultado da validação
     */
    function validateFinancingForm(event) {
        let isValid = true;
        const form = event.target;

        // Valida descrição
        const nameField = form.querySelector('#Name');
        if (nameField && nameField.value.trim() === '') {
            isValid = false;
            showFieldError(nameField, 'O nome do financiamento é obrigatório');
        }

        // Valida valor total
        const totalAmountField = form.querySelector('#TotalAmount');
        if (totalAmountField) {
            const totalAmount = parseCurrency(totalAmountField.value);
            if (isNaN(totalAmount) || totalAmount <= 0) {
                isValid = false;
                showFieldError(totalAmountField, 'Informe um valor total válido');
            }
        }

        // Valida taxa de juros
        const interestRateField = form.querySelector('#InterestRate');
        if (interestRateField) {
            const interestRate = parsePercent(interestRateField.value);
            if (isNaN(interestRate) || interestRate < 0 || interestRate > 100) {
                isValid = false;
                showFieldError(interestRateField, 'A taxa de juros deve estar entre 0 e 100%');
            }
        }

        // Valida prazo
        const termMonthsField = form.querySelector('#TermMonths');
        if (termMonthsField) {
            const termMonths = parseInt(termMonthsField.value);
            if (isNaN(termMonths) || termMonths <= 0 || termMonths > 600) {
                isValid = false;
                showFieldError(termMonthsField, 'O prazo deve estar entre 1 e 600 meses');
            }
        }

        // Valida tipo de financiamento
        const typeField = form.querySelector('#Type');
        if (typeField && typeField.value === '') {
            isValid = false;
            showFieldError(typeField, 'Selecione o tipo de financiamento');
        }

        return isValid;
    }

    /**
     * Mostra mensagem de erro para um campo
     * @param {HTMLElement} input - Campo com erro
     * @param {string} message - Mensagem de erro
     */
    function showFieldError(input, message) {
        // Usa o módulo de validação se disponível
        if (FinanceSystem.Validation && FinanceSystem.Validation.showFieldError) {
            FinanceSystem.Validation.showFieldError(input, message);
            return;
        }

        // Fallback para exibição manual de erro
        let errorElement = input.parentElement.querySelector('.text-danger');
        if (!errorElement) {
            errorElement = document.createElement('span');
            errorElement.classList.add('text-danger');
            input.parentElement.appendChild(errorElement);
        }
        errorElement.innerText = message;
        input.classList.add('is-invalid');
    }

    /**
     * Remove mensagem de erro de um campo
     * @param {HTMLElement} input - Campo
     */
    function clearFieldError(input) {
        // Usa o módulo de validação se disponível
        if (FinanceSystem.Validation && FinanceSystem.Validation.clearFieldError) {
            FinanceSystem.Validation.clearFieldError(input);
            return;
        }

        // Fallback para limpeza manual de erro
        const errorElement = input.parentElement.querySelector('.text-danger');
        if (errorElement) {
            errorElement.innerText = '';
        }
        input.classList.remove('is-invalid');
    }

    /**
     * Inicializa a lista de financiamentos
     */
    function initializeFinancingsList() {
        // Inicializa DataTables se disponível
        initializeFinancingsDataTable();

        // Inicializa botões de ação
        initializeFinancingActionButtons();
    }

    /**
     * Inicializa DataTables para a tabela de financiamentos
     */
    function initializeFinancingsDataTable() {
        // Verifica se DataTables está disponível
        if (typeof $.fn.DataTable !== 'undefined') {
            $('.financings-table').DataTable({
                language: {
                    url: '//cdn.datatables.net/plug-ins/1.10.25/i18n/Portuguese-Brasil.json'
                },
                responsive: true,
                pageLength: 10,
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "Todos"]],
                order: [[3, 'desc']], // Ordena por data de aquisição decrescente
                columnDefs: [
                    { orderable: false, targets: -1 } // Desabilita ordenação na coluna de ações
                ]
            });
        } else if (FinanceSystem.Modules && FinanceSystem.Modules.Tables) {
            // Usa o módulo Tables se DataTables não estiver disponível
            FinanceSystem.Modules.Tables.initializeTableSort();
        }
    }

    /**
     * Inicializa botões de ação para financiamentos
     */
    function initializeFinancingActionButtons() {
        // Botões de exclusão
        const deleteButtons = document.querySelectorAll('.btn-delete-financing');
        deleteButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                if (!confirm('Tem certeza que deseja excluir este financiamento? Esta ação não pode ser desfeita.')) {
                    e.preventDefault();
                }
            });
        });

        // Botões de correção monetária
        const correctionButtons = document.querySelectorAll('.btn-apply-correction');
        correctionButtons.forEach(button => {
            button.addEventListener('click', function () {
                const financingId = this.getAttribute('data-financing-id');
                openCorrectionModal(financingId);
            });
        });
    }

    /**
     * Abre modal para correção monetária
     * @param {string} financingId - ID do financiamento
     */
    function openCorrectionModal(financingId) {
        const modal = document.getElementById('correctionModal');
        const financingIdInput = document.getElementById('FinancingId');

        if (modal && financingIdInput) {
            financingIdInput.value = financingId;

            // Usa o módulo UI se disponível
            if (FinanceSystem.UI && FinanceSystem.UI.showModal) {
                FinanceSystem.UI.showModal('correctionModal');
            } else if (typeof bootstrap !== 'undefined') {
                // Fallback para Bootstrap
                const modalInstance = new bootstrap.Modal(modal);
                modalInstance.show();
            } else {
                // Fallback básico
                modal.style.display = 'block';
            }
        }
    }

    /**
     * Inicializa a página de detalhes do financiamento
     */
    function initializeFinancingDetails() {
        // Inicializa gráficos
        initializeFinancingCharts();

        // Inicializa tabela de parcelas
        const installmentsTable = document.querySelector('.installments-table');
        if (installmentsTable) {
            initializeInstallmentsTable();
        }

        // Inicializa ações para parcelas
        initializeInstallmentActions();
    }

    /**
     * Inicializa gráficos na página de detalhes
     */
    function initializeFinancingCharts() {
        // Gráfico de progresso
        const progressChart = document.getElementById('progressChart');
        if (progressChart) {
            const percentage = parseFloat(progressChart.getAttribute('data-progress') || '0');

            // Usa o módulo Charts se disponível
            if (FinanceSystem.Modules && FinanceSystem.Modules.Charts) {
                FinanceSystem.Modules.Charts.createDoughnutChart('progressChart',
                    ['Pago', 'Restante'],
                    [percentage, 100 - percentage],
                    {
                        cutout: '70%',
                        colors: ['rgba(28, 200, 138, 0.8)', 'rgba(220, 220, 220, 0.8)']
                    }
                );
            } else if (typeof Chart !== 'undefined') {
                // Fallback para Chart.js diretamente
                createProgressDoughnut(progressChart, percentage);
            }

            // Mostrar percentual no centro
            const progressLabel = document.getElementById('progressLabel');
            if (progressLabel) {
                progressLabel.textContent = `${percentage.toFixed(0)}%`;
            }
        }

        // Gráfico de distribuição principal/juros
        const distributionChart = document.getElementById('distributionChart');
        if (distributionChart) {
            const principal = parseFloat(distributionChart.getAttribute('data-principal') || '0');
            const interest = parseFloat(distributionChart.getAttribute('data-interest') || '0');

            // Usa o módulo Charts se disponível
            if (FinanceSystem.Modules && FinanceSystem.Modules.Charts) {
                FinanceSystem.Modules.Charts.createPieChart('distributionChart',
                    ['Principal', 'Juros'],
                    [principal, interest],
                    {
                        colors: ['rgba(78, 115, 223, 0.8)', 'rgba(231, 74, 59, 0.8)']
                    }
                );
            } else if (typeof Chart !== 'undefined') {
                // Fallback para Chart.js diretamente
                createDistributionPieChart(distributionChart, principal, interest);
            }
        }
    }

    /**
     * Cria gráfico de donut para progresso (fallback)
     * @param {HTMLCanvasElement} canvas - Elemento canvas
     * @param {number} percentage - Percentual de progresso
     */
    function createProgressDoughnut(canvas, percentage) {
        new Chart(canvas, {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: [percentage, 100 - percentage],
                    backgroundColor: [
                        'rgba(28, 200, 138, 0.8)',
                        'rgba(220, 220, 220, 0.8)'
                    ],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '70%',
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        enabled: false
                    }
                }
            }
        });
    }

    /**
     * Cria gráfico de pizza para distribuição principal/juros (fallback)
     * @param {HTMLCanvasElement} canvas - Elemento canvas
     * @param {number} principal - Valor principal
     * @param {number} interest - Valor de juros
     */
    function createDistributionPieChart(canvas, principal, interest) {
        new Chart(canvas, {
            type: 'pie',
            data: {
                labels: ['Principal', 'Juros'],
                datasets: [{
                    data: [principal, interest],
                    backgroundColor: [
                        'rgba(78, 115, 223, 0.8)',
                        'rgba(231, 74, 59, 0.8)'
                    ],
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const value = context.raw;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((value / total) * 100).toFixed(1);
                                return `${context.label}: R$ ${value.toLocaleString('pt-BR')} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Inicializa tabela de parcelas
     */
    function initializeInstallmentsTable() {
        // Usa DataTables se disponível
        if (typeof $.fn.DataTable !== 'undefined') {
            $('.installments-table').DataTable({
                language: {
                    url: '//cdn.datatables.net/plug-ins/1.10.25/i18n/Portuguese-Brasil.json'
                },
                responsive: true,
                pageLength: 10,
                order: [[0, 'asc']], // Ordena por número da parcela crescente
                columnDefs: [
                    { orderable: false, targets: -1 } // Desabilita ordenação na coluna de ações
                ]
            });
        } else if (FinanceSystem.Modules && FinanceSystem.Modules.Tables) {
            // Usa o módulo Tables se DataTables não estiver disponível
            FinanceSystem.Modules.Tables.initializeTableSort();
        }
    }

    /**
     * Inicializa ações em parcelas
     */
    function initializeInstallmentActions() {
        // Botões para pagar parcela
        const payButtons = document.querySelectorAll('.pay-installment');
        payButtons.forEach(button => {
            button.addEventListener('click', function () {
                const installmentId = this.getAttribute('data-installment-id');
                openPayInstallmentModal(installmentId);
            });
        });
    }

    /**
     * Abre modal para pagar parcela
     * @param {string} installmentId - ID da parcela
     */
    function openPayInstallmentModal(installmentId) {
        const modal = document.getElementById('payInstallmentModal');
        const installmentIdInput = document.getElementById('InstallmentId');

        if (modal && installmentIdInput) {
            installmentIdInput.value = installmentId;

            // Usa o módulo UI se disponível
            if (FinanceSystem.UI && FinanceSystem.UI.showModal) {
                FinanceSystem.UI.showModal('payInstallmentModal');
            } else if (typeof bootstrap !== 'undefined') {
                // Fallback para Bootstrap
                const modalInstance = new bootstrap.Modal(modal);
                modalInstance.show();
            } else {
                // Fallback básico
                modal.style.display = 'block';
            }
        }
    }

    /**
     * Inicializa a página de simulação de financiamento
     */
    function initializeFinancingSimulation() {
        // Inicializa máscaras para campos monetários
        initializeMoneyMasks();

        // Inicializa calculadora de parcelas
        initializeInstallmentCalculator();

        // Configura botão de simulação
        const simulateButton = document.getElementById('simulateButton');
        if (simulateButton) {
            simulateButton.addEventListener('click', function (e) {
                // Validar antes de enviar
                const totalAmountField = document.getElementById('TotalAmount');
                const interestRateField = document.getElementById('InterestRate');
                const termMonthsField = document.getElementById('TermMonths');

                if (!totalAmountField.value || !interestRateField.value || !termMonthsField.value) {
                    e.preventDefault();
                    alert('Preencha todos os campos obrigatórios');
                }
            });
        }

        // Inicializa tabela de resultados da simulação se existir
        const simulationResultsTable = document.querySelector('.simulation-results-table');
        if (simulationResultsTable) {
            initializeSimulationResultsTable();
        }
    }

    /**
     * Inicializa tabela de resultados da simulação
     */
    function initializeSimulationResultsTable() {
        // Usa DataTables se disponível
        if (typeof $.fn.DataTable !== 'undefined') {
            $('.simulation-results-table').DataTable({
                language: {
                    url: '//cdn.datatables.net/plug-ins/1.10.25/i18n/Portuguese-Brasil.json'
                },
                responsive: true,
                pageLength: 12,
                lengthMenu: [[12, 24, 36, -1], [12, 24, 36, "Todos"]],
                dom: 'Bfrtip',
                buttons: [
                    {
                        extend: 'print',
                        text: 'Imprimir',
                        className: 'btn btn-sm btn-primary'
                    },
                    {
                        extend: 'excel',
                        text: 'Excel',
                        className: 'btn btn-sm btn-success'
                    },
                    {
                        extend: 'pdf',
                        text: 'PDF',
                        className: 'btn btn-sm btn-danger'
                    }
                ]
            });
        } else if (FinanceSystem.Modules && FinanceSystem.Modules.Tables) {
            // Usa o módulo Tables se DataTables não estiver disponível
            FinanceSystem.Modules.Tables.initializeTableSort();
        }

        // Inicializa gráficos de simulação
        initializeSimulationCharts();
    }

    /**
     * Inicializa gráficos da simulação
     */
    function initializeSimulationCharts() {
        // Gráfico de amortização e juros
        const amortizationChart = document.getElementById('amortizationChart');
        if (amortizationChart) {
            // Obter dados dos atributos data-*
            const labelsRaw = amortizationChart.getAttribute('data-labels');
            const principalRaw = amortizationChart.getAttribute('data-principal');
            const interestRaw = amortizationChart.getAttribute('data-interest');

            if (labelsRaw && principalRaw && interestRaw) {
                const labels = JSON.parse(labelsRaw);
                const principal = JSON.parse(principalRaw);
                const interest = JSON.parse(interestRaw);

                // Usar o módulo Charts se disponível
                if (FinanceSystem.Modules && FinanceSystem.Modules.Charts) {
                    FinanceSystem.Modules.Charts.createGroupedBarChart('amortizationChart', labels, [
                        {
                            label: 'Principal',
                            data: principal,
                            color: 'rgba(78, 115, 223, 0.8)'
                        },
                        {
                            label: 'Juros',
                            data: interest,
                            color: 'rgba(231, 74, 59, 0.8)'
                        }
                    ]);
                } else if (typeof Chart !== 'undefined') {
                    // Fallback para Chart.js diretamente
                    createAmortizationChart(amortizationChart, labels, principal, interest);
                }
            }
        }

        // Gráfico de saldo devedor
        const balanceChart = document.getElementById('balanceChart');
        if (balanceChart) {
            const labelsRaw = balanceChart.getAttribute('data-labels');
            const valuesRaw = balanceChart.getAttribute('data-values');

            if (labelsRaw && valuesRaw) {
                const labels = JSON.parse(labelsRaw);
                const values = JSON.parse(valuesRaw);

                // Usar o módulo Charts se disponível
                if (FinanceSystem.Modules && FinanceSystem.Modules.Charts) {
                    FinanceSystem.Modules.Charts.createLineChart('balanceChart', labels, values);
                } else if (typeof Chart !== 'undefined') {
                    // Fallback para Chart.js diretamente
                    createBalanceChart(balanceChart, labels, values);
                }
            }
        }
    }

    /**
     * Cria gráfico de amortização e juros (fallback)
     * @param {HTMLCanvasElement} canvas - Elemento canvas
     * @param {Array} labels - Rótulos dos meses
     * @param {Array} principal - Valores de amortização
     * @param {Array} interest - Valores de juros
     */
    function createAmortizationChart(canvas, labels, principal, interest) {
        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Principal',
                        data: principal,
                        backgroundColor: 'rgba(78, 115, 223, 0.8)',
                        borderColor: 'rgba(78, 115, 223, 1)',
                        borderWidth: 1
                    },
                    {
                        label: 'Juros',
                        data: interest,
                        backgroundColor: 'rgba(231, 74, 59, 0.8)',
                        borderColor: 'rgba(231, 74, 59, 1)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: {
                        stacked: false,
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        stacked: false,
                        ticks: {
                            callback: function (value) {
                                return 'R$ ' + value.toLocaleString('pt-BR');
                            }
                        }
                    }
                },
                plugins: {
                    legend: {
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return context.dataset.label + ': R$ ' + context.raw.toLocaleString('pt-BR');
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Cria gráfico de saldo devedor (fallback)
     * @param {HTMLCanvasElement} canvas - Elemento canvas
     * @param {Array} labels - Rótulos dos meses
     * @param {Array} values - Valores do saldo devedor
     */
    function createBalanceChart(canvas, labels, values) {
        new Chart(canvas, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Saldo Devedor',
                    data: values,
                    backgroundColor: 'rgba(78, 115, 223, 0.05)',
                    borderColor: 'rgba(78, 115, 223, 1)',
                    pointBackgroundColor: 'rgba(78, 115, 223, 1)',
                    pointBorderColor: '#fff',
                    pointHoverBackgroundColor: '#fff',
                    pointHoverBorderColor: 'rgba(78, 115, 223, 1)',
                    pointRadius: 3,
                    pointHoverRadius: 5,
                    tension: 0.3,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        ticks: {
                            callback: function (value) {
                                return 'R$ ' + value.toLocaleString('pt-BR');
                            }
                        }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return 'Saldo: R$ ' + context.raw.toLocaleString('pt-BR');
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Converte valor monetário para número
     * @param {string} value - Valor em formato monetário
     * @returns {number} - Valor numérico
     */
    function parseCurrency(value) {
        // Usa o módulo Financial se disponível
        if (FinanceSystem.Modules && FinanceSystem.Modules.Financial) {
            return FinanceSystem.Modules.Financial.parseCurrency(value);
        }

        // Implementação manual
        if (typeof value === 'number') {
            return value;
        }

        // Remove símbolos de moeda e espaços
        value = value.replace(/[^\d,.-]/g, '');

        // Trata formato brasileiro (1.234,56)
        if (value.indexOf(',') > -1 && value.indexOf('.') > -1) {
            value = value.replace(/\./g, '').replace(',', '.');
        } else if (value.indexOf(',') > -1) {
            value = value.replace(',', '.');
        }

        return parseFloat(value);
    }

    /**
     * Converte valor percentual para número
     * @param {string} value - Valor em formato percentual
     * @returns {number} - Valor numérico (decimal)
     */
    function parsePercent(value) {
        if (typeof value === 'number') {
            return value;
        }

        // Remove o símbolo de percentual e espaços
        value = value.replace(/[^\d,.-]/g, '');

        // Substitui vírgula por ponto
        value = value.replace(',', '.');

        // Converte para decimal (divide por 100)
        return parseFloat(value) / 100;
    }

    /**
     * Formata um número como moeda
     * @param {number} value - Valor a ser formatado
     * @returns {string} - Valor formatado
     */
    function formatNumber(value) {
        return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // API pública do módulo
    return {
        initialize: initialize,
        initializeFinancingForm: initializeFinancingForm,
        initializeFinancingsList: initializeFinancingsList,
        initializeFinancingDetails: initializeFinancingDetails,
        initializeFinancingSimulation: initializeFinancingSimulation,
        calculateInstallment: calculateInstallment
    };
})();