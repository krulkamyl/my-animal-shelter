<?php

namespace Tests\Feature\Repositories;

use App\Models\Shelter;
use App\Repositories\Shelter\ShelterRepository;
use Database\Factories\AddressFactory;
use Database\Factories\ShelterFactory;
use Illuminate\Foundation\Testing\DatabaseTransactions;
use Tests\TestCase;

class ShelterRepositoryTest extends TestCase
{
    use DatabaseTransactions;

    public ShelterRepository $shelterRepository;

    public function __construct(string $name)
    {
        $this->shelterRepository = new ShelterRepository(
            new Shelter()
        );
        parent::__construct($name);
    }

    public function testGetById(): void
    {
        $shelter = ShelterFactory::prepare();
        $shelterFromRepository = $this->shelterRepository->getById($shelter->id);

        $this->assertEquals($shelter->id, $shelterFromRepository->id);
        $this->assertEquals($shelter->name, $shelterFromRepository->name);
    }

    public function testGetAll(): void
    {
        $shelters = ShelterFactory::times(5)->create();
        $sheltersFromRepository = $this->shelterRepository->getAll();

        $this->assertEquals($shelters->count(), $sheltersFromRepository->count());
    }

    public function testCreate(): void
    {
        $shelter = (new ShelterFactory())->definition();
        $shelter['address'] = (new AddressFactory())->definition();

        $shelterFromRepository = $this->shelterRepository->create($shelter);

        $this->assertEquals($shelter['name'], $shelterFromRepository->name);
    }

    public function testUpdate(): void
    {
        $shelter = ShelterFactory::prepare();

        $shelterUpdated = $shelter->toArray();
        $shelterUpdated['name'] = 'Updated Name';
        $shelterUpdated['address'] = $shelter->address->toArray();

        $shelterFromRepository = $this->shelterRepository->update($shelter->id, $shelterUpdated);

        $this->assertEquals($shelterUpdated['name'], $shelterFromRepository->name);
    }
}
